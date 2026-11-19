using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinkedInContentManager.Application.ExternalServices.CrewAi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedInContentManager.Infrastructure.ExternalServices.CrewAi;

/// <summary>
/// Strongly-typed client for the CrewAI FastAPI service's <c>POST /api/v1/execute</c>.
/// Retry/circuit-breaker/timeout behavior is NOT implemented here — it's configured once on
/// the <see cref="HttpClient"/> pipeline via <c>AddStandardResilienceHandler</c> in
/// <see cref="DependencyInjection.AddInfrastructureServices"/>, so this class only has to
/// worry about building the request, propagating the correlation ID, and mapping the response.
/// </summary>
public sealed class CrewAiHttpClient(HttpClient httpClient, ILogger<CrewAiHttpClient> logger) : ICrewAiClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<CrewAiExecutionResult> ExecuteContentPipelineAsync(CrewAiExecutionRequest request, CancellationToken cancellationToken)
    {
        var wireRequest = new CrewAiExecuteRequestWire(request.Topic, request.Niche, request.Persona, request.CorrelationId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/execute")
        {
            Content = JsonContent.Create(wireRequest, options: JsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            httpRequest.Headers.Add("X-Correlation-Id", request.CorrelationId);

        logger.LogInformation(
            "Calling CrewAI service: topic={Topic} niche={Niche} persona={Persona} correlationId={CorrelationId}",
            request.Topic, request.Niche, request.Persona, request.CorrelationId);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "CrewAI service call failed (transport) for correlationId={CorrelationId}", request.CorrelationId);
            throw new CrewAiServiceException("CrewAI service is unreachable or timed out.", innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken);
            logger.LogError(
                "CrewAI service returned {StatusCode} for correlationId={CorrelationId}: {Body}",
                (int)response.StatusCode, request.CorrelationId, body);

            throw new CrewAiServiceException(
                $"CrewAI service returned {(int)response.StatusCode} {response.StatusCode}.",
                statusCode: (int)response.StatusCode);
        }

        var wireResponse = await response.Content.ReadFromJsonAsync<CrewAiExecuteResponseWire>(JsonOptions, cancellationToken)
            ?? throw new CrewAiServiceException("CrewAI service returned an empty response body.");

        return MapToResult(wireResponse);
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return text.Length > 2000 ? text[..2000] : text;
        }
        catch
        {
            return "<unreadable response body>";
        }
    }

    private static CrewAiExecutionResult MapToResult(CrewAiExecuteResponseWire wire) => new(
        wire.ExecutionId,
        wire.Status,
        wire.Research is null ? null : new CrewAiResearchOutput(
            wire.Research.TrendingContentAngles, wire.Research.TrendingHashtags,
            wire.Research.TrendingHooks, wire.Research.SourceUrls, wire.Research.Summary),
        wire.Writing is null ? null : new CrewAiDraftOutput(wire.Writing.DraftContent, wire.Writing.WordCount, wire.Writing.SuggestedHashtags),
        wire.Critique is null ? null : new CrewAiCritiqueOutput(
            wire.Critique.Score, wire.Critique.HookEvaluation, wire.Critique.StorytellingEvaluation,
            wire.Critique.EngagementPotential, wire.Critique.CtaEffectiveness, wire.Critique.ToneReadabilityAssessment,
            wire.Critique.GuardrailViolations, wire.Critique.Recommendations),
        wire.Optimization is null ? null : new CrewAiOptimizationOutput(
            wire.Optimization.OptimizedContent, wire.Optimization.WordCount, wire.Optimization.OptimizedHashtags),
        wire.Scheduling is null ? null : new CrewAiSchedulingOutput(
            wire.Scheduling.RecommendedDay, wire.Scheduling.RecommendedTime, wire.Scheduling.AudienceTimezone,
            wire.Scheduling.FinalPost, wire.Scheduling.HashtagStrategy, wire.Scheduling.FirstHourEngagementPlan),
        wire.FinalPublishingBrief,
        wire.Error);
}
