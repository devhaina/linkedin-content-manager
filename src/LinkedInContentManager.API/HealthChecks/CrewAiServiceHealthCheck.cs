using LinkedInContentManager.Infrastructure.ExternalServices.CrewAi;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LinkedInContentManager.API.HealthChecks;

/// <summary>Pings the CrewAI FastAPI service's own <c>/health</c> endpoint — real HTTP call, not a TCP/ping stub.</summary>
public sealed class CrewAiServiceHealthCheck(IHttpClientFactory httpClientFactory, IOptions<CrewAiServiceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("CrewAI service responded healthy.")
                : HealthCheckResult.Unhealthy($"CrewAI service returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CrewAI service is unreachable.", ex);
        }
    }
}
