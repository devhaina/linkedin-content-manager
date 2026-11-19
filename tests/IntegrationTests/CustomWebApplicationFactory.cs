using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using LinkedInContentManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace LinkedInContentManager.IntegrationTests;

/// <summary>
/// Spins up a REAL, isolated PostgreSQL container per test run (Testcontainers — not an
/// in-memory provider, which doesn't enforce the same constraints/behavior), applies the real
/// EF Core migrations, and replaces only the two external dependencies these fast API tests
/// should never hit for real: <see cref="ICrewAiClient"/> (a live run costs real money and
/// takes minutes — that's covered separately by <see cref="CrewAiHttpClientTests"/>) and
/// <see cref="IContentGenerationJobPublisher"/> (no RabbitMQ needed for Campaigns/Auth/Schedules
/// CRUD tests). Everything else — Postgres, the full DI graph, real JWT issuance/validation,
/// real password hashing — is the genuine article.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("linkedin_content_manager_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public FakeCrewAiClient FakeCrewAiClient { get; } = new();
    public FakeContentGenerationJobPublisher FakePublisher { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["CrewAiService:BaseUrl"] = "http://localhost:8000",
                ["RabbitMq:HostName"] = "localhost",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:UserName"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:VirtualHost"] = "/",
                ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "LinkedInContentManager.Tests",
                ["Jwt:Audience"] = "LinkedInContentManager.Tests.Clients",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICrewAiClient>();
            services.AddSingleton<ICrewAiClient>(FakeCrewAiClient);

            services.RemoveAll<IContentGenerationJobPublisher>();
            services.AddSingleton<IContentGenerationJobPublisher>(FakePublisher);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Force Server/client creation now (via a throwaway scope) so the migration runs
        // before any test issues a request.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
