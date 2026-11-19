using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Infrastructure.ExternalServices.CrewAi;

/// <summary>
/// JSON wire shapes for the CrewAI FastAPI service, kept private to Infrastructure. Property
/// names are serialized snake_case (see <see cref="CrewAiHttpClient.JsonOptions"/>) to match
/// the Python side's Pydantic field names exactly — these are NOT the Application-layer DTOs.
/// </summary>
internal sealed record CrewAiExecuteRequestWire(string Topic, string Niche, WriterPersona Persona, string? CorrelationId);

internal sealed record CrewAiExecuteResponseWire(
    string ExecutionId,
    string Status,
    CrewAiResearchWire? Research,
    CrewAiDraftWire? Writing,
    CrewAiCritiqueWire? Critique,
    CrewAiOptimizationWire? Optimization,
    CrewAiSchedulingWire? Scheduling,
    string? FinalPublishingBrief,
    string? Error);

internal sealed record CrewAiResearchWire(
    List<string> TrendingContentAngles,
    List<string> TrendingHashtags,
    List<string> TrendingHooks,
    List<string> SourceUrls,
    string Summary);

internal sealed record CrewAiDraftWire(string DraftContent, int WordCount, List<string> SuggestedHashtags);

internal sealed record CrewAiCritiqueWire(
    int Score,
    string HookEvaluation,
    string StorytellingEvaluation,
    string EngagementPotential,
    string CtaEffectiveness,
    string ToneReadabilityAssessment,
    List<string> GuardrailViolations,
    List<string> Recommendations);

internal sealed record CrewAiOptimizationWire(string OptimizedContent, int WordCount, List<string> OptimizedHashtags);

internal sealed record CrewAiSchedulingWire(
    string RecommendedDay,
    string RecommendedTime,
    string AudienceTimezone,
    string FinalPost,
    string HashtagStrategy,
    string FirstHourEngagementPlan);
