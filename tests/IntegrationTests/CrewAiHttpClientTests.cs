using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedInContentManager.IntegrationTests;

/// <summary>
/// Exercises the real CrewAI HTTP client pipeline (HttpClientFactory, resilience
/// handler, correlation-id header, snake_case JSON mapping) against an actually-running
/// CrewAI service — no mocks. Requires `uvicorn app.main:app` running on localhost:8000 with a
/// real OPENAI_API_KEY/SERPER_API_KEY configured; skipped otherwise rather than faking a pass.
/// A full CI story (fast unit tests vs. this live-dependency category) is formalized in Phase 7.
/// </summary>
public class CrewAiHttpClientTests
{
    [SkippableFact(Timeout = 180_000)]
    public async Task ExecuteContentPipelineAsync_AgainstLiveCrewAiService_ReturnsCompletedResultWithAllStages()
    {
        // Explicitly skipped (reported as Skipped, not Passed) when the live dependency isn't
        // reachable — a live-dependency test that can't reach its dependency must say so,
        // never report green by doing nothing.
        Skip.IfNot(await IsCrewAiServiceRunningAsync(),
            "CrewAI service is not reachable at http://localhost:8000 — start it with " +
            "'uvicorn app.main:app --host 0.0.0.0 --port 8000' from crewai-service/ to run this test.");

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = "Host=localhost;Port=5432;Database=unused_for_this_test;Username=postgres;Password=postgres",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["CrewAiService:BaseUrl"] = "http://localhost:8000",
                ["CrewAiService:TimeoutSeconds"] = "180",
                ["Jwt:SigningKey"] = "test-signing-key-not-used-but-required-32-chars-min",
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test"
            })
            .Build();

        services.AddInfrastructureServices(configuration);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ICrewAiClient>();

        var result = await client.ExecuteContentPipelineAsync(
            new CrewAiExecutionRequest(
                Topic: "Passwordless authentication adoption in digital banking",
                Niche: "Fintech Security",
                Persona: WriterPersona.Practitioner,
                CorrelationId: Guid.NewGuid().ToString()),
            CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.NotNull(result.Research);
        Assert.NotEmpty(result.Research!.SourceUrls);
        Assert.NotNull(result.Writing);
        Assert.NotNull(result.Critique);
        Assert.InRange(result.Critique!.Score, 0, 10);
        Assert.NotNull(result.Optimization);
        Assert.NotNull(result.Scheduling);
        Assert.False(string.IsNullOrWhiteSpace(result.FinalPublishingBrief));
    }

    private static async Task<bool> IsCrewAiServiceRunningAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await client.GetAsync("http://localhost:8000/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
