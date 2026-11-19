namespace LinkedInContentManager.Application.ExternalServices.CrewAi;

/// <summary>
/// Every stage's output from one CrewAI pipeline run, mirroring the Python service's
/// <c>ExecutionResponse</c> schema one-for-one. <see cref="Status"/> is the raw string the
/// service returned ("Completed"/"Failed") — mapping it onto
/// <see cref="LinkedInContentManager.Domain.Enums.ExecutionStatus"/> is an Application-handler
/// concern (Phase 5), not this client's.
/// </summary>
public sealed record CrewAiExecutionResult(
    string ExecutionId,
    string Status,
    CrewAiResearchOutput? Research,
    CrewAiDraftOutput? Writing,
    CrewAiCritiqueOutput? Critique,
    CrewAiOptimizationOutput? Optimization,
    CrewAiSchedulingOutput? Scheduling,
    string? FinalPublishingBrief,
    string? Error);

public sealed record CrewAiResearchOutput(
    List<string> TrendingContentAngles,
    List<string> TrendingHashtags,
    List<string> TrendingHooks,
    List<string> SourceUrls,
    string Summary);

public sealed record CrewAiDraftOutput(string DraftContent, int WordCount, List<string> SuggestedHashtags);

public sealed record CrewAiCritiqueOutput(
    int Score,
    string HookEvaluation,
    string StorytellingEvaluation,
    string EngagementPotential,
    string CtaEffectiveness,
    string ToneReadabilityAssessment,
    List<string> GuardrailViolations,
    List<string> Recommendations);

public sealed record CrewAiOptimizationOutput(string OptimizedContent, int WordCount, List<string> OptimizedHashtags);

public sealed record CrewAiSchedulingOutput(
    string RecommendedDay,
    string RecommendedTime,
    string AudienceTimezone,
    string FinalPost,
    string HashtagStrategy,
    string FirstHourEngagementPlan);
