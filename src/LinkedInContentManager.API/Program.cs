using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LinkedInContentManager.API.ExceptionHandling;
using LinkedInContentManager.API.HealthChecks;
using LinkedInContentManager.API.Services;
using LinkedInContentManager.Application;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog: structured logging, enriched with correlation IDs ----
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console());

// ---- Core services ----
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "LinkedIn Content Manager API",
        Version = "v1",
        Description = "Campaign, content-generation, approval, and scheduling API for the autonomous LinkedIn content pipeline.",
    });
    // Adds the "Authorize" padlock in Swagger UI so a JWT can be attached manually to try
    // protected endpoints. (Auto-applying it per-[Authorize]-endpoint needs
    // OpenApiSecuritySchemeReference, which requires the in-progress OpenApiDocument instance
    // — Microsoft.OpenApi 3.x's security-requirement API changed shape significantly from the
    // 1.x/2.x version most Swashbuckle examples still show; not worth the complexity for what
    // is ultimately a UI convenience, not a deliverable in its own right.)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Paste a JWT access token from POST /api/auth/login — no need to type \"Bearer \" first.",
    });
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- JWT authentication + role-based authorization ----
// Deliberately NOT read into a local variable here and closed over below: doing that reads
// `builder.Configuration` once, at this exact line, and freezes the value — any configuration
// provider added afterward (exactly what a WebApplicationFactory-based integration test does
// to inject a test signing key) is silently ignored. That previously caused a real,
// hard-to-diagnose bug: the token-issuing side (ITokenService, bound via the lazy IOptions
// pattern) picked up the test's overridden key while this validation side kept using whatever
// key was present at startup — a genuine signature mismatch, 401 on every authenticated call
// despite a perfectly valid, freshly-issued token. Resolving IConfiguration fresh via DI inside
// Configure<IConfiguration> (like the Postgres/Redis connection strings in
// Infrastructure.DependencyInjection) reads the final, fully-assembled configuration instead.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(_ => { });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set it via `dotnet user-secrets set \"Jwt:SigningKey\" \"<a long random value>\"` " +
                "locally, or the Jwt__SigningKey environment variable in any deployed environment. It must never be committed to appsettings.json.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// ---- CORS (for the Angular dashboard) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---- Rate limiting: per-client-IP fixed window, generous enough for normal dashboard use ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// ---- Health checks: Postgres, Redis, RabbitMQ, CrewAI service ----
// Every connection string/host setting here is resolved from `sp`'s IConfiguration inside the
// lambda, not from the `builder.Configuration` closure — see the JWT comment above for why
// that distinction matters for anything a test might override.
builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("PostgreSql")!, name: "postgresql")
    .AddRedis(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis")!, name: "redis")
    .AddRabbitMQ(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"]!,
            Port = int.Parse(configuration["RabbitMq:Port"]!),
            UserName = configuration["RabbitMq:UserName"]!,
            Password = configuration["RabbitMq:Password"]!,
            VirtualHost = configuration["RabbitMq:VirtualHost"]!,
        };
        return factory.CreateConnectionAsync();
    }, name: "rabbitmq")
    .AddCheck<CrewAiServiceHealthCheck>("crewai-service");

// ---- OpenTelemetry tracing ----
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("LinkedInContentManager.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")));

var app = builder.Build();

// Applies any pending EF Core migrations on startup. Fine for this deployment's shape (one
// API instance, no rolling/blue-green deploys) — a separate one-shot migrator step would be
// the right call for a multi-instance production deployment, to avoid two instances racing to
// migrate at once; see README for that trade-off.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LinkedInContentManager.Infrastructure.Persistence.ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger UI stays available in every environment — this is an internal/B2B API, not a
// public-facing one, and the interactive docs are genuinely useful in staging/production too.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkedIn Content Manager API v1");
    options.RoutePrefix = "swagger";
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = LinkedInContentManager.API.HealthChecks.HealthCheckResponseWriter.WriteAsync,
});

app.Run();
