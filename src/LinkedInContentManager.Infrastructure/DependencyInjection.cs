using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using LinkedInContentManager.Infrastructure.ExternalServices.Auth;
using LinkedInContentManager.Infrastructure.ExternalServices.CrewAi;
using LinkedInContentManager.Infrastructure.ExternalServices.Messaging;
using LinkedInContentManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace LinkedInContentManager.Infrastructure;

/// <summary>Registers everything the Infrastructure layer owns: EF Core/Postgres and the CrewAI HTTP client.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily from the DI-provided IConfiguration (not the `configuration`
        // parameter captured as a frozen local) — reading `GetConnectionString(...)` into a
        // local eagerly, at registration time, silently ignores any configuration providers
        // added afterward (exactly what WebApplicationFactory-based integration tests do to
        // override the connection string). Resolving inside the (sp, options) callback reads
        // the final, fully-assembled configuration at first DbContext resolution instead.
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("PostgreSql")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is not configured.");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddOptions<CrewAiServiceOptions>()
            .Bind(configuration.GetSection(CrewAiServiceOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpClient<ICrewAiClient, CrewAiHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CrewAiServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Add("X-Service-Api-Key", options.ApiKey);
        })
        .AddStandardResilienceHandler(resilience =>
        {
            // CrewAI runs are long (multiple sequential GPT-4o calls) — the per-attempt
            // timeout must stay below the overall HttpClient.Timeout set above, and the
            // total resilience-pipeline timeout must be >= the per-attempt timeout.
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(170);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(170);
            resilience.Retry.MaxRetryAttempts = 2;
            resilience.Retry.BackoffType = DelayBackoffType.Exponential;
            resilience.CircuitBreaker.FailureRatio = 0.5;
            resilience.CircuitBreaker.MinimumThroughput = 5;
            // Must be >= 2x the attempt timeout above (Polly's own validation rule) for the
            // circuit breaker's failure-rate sampling to be statistically meaningful.
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(360);
        });

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<RabbitMqConnectionManager>();
        services.AddSingleton<IContentGenerationJobPublisher, RabbitMqContentGenerationJobPublisher>();

        services.AddStackExchangeRedisCache(_ => { }); // configured below, lazily, for the same reason as the DbContext above
        services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
            .Configure<IConfiguration>((options, config) => options.Configuration = config.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured."));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
