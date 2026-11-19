using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Contracts.Content;

public sealed record GenerateContentRequest(Guid CampaignId, string Topic, WriterPersona? PersonaOverride = null);

/// <summary>Returned immediately — generation runs asynchronously via the RabbitMQ worker. Poll <c>GET /api/content/{id}</c> for status.</summary>
public sealed record GenerateContentResponse(Guid ExecutionId, ExecutionStatus Status);

public sealed record RegenerateContentRequest(WriterPersona? PersonaOverride = null);

public sealed record ResearchBriefDto(
    List<string> TrendingContentAngles,
    List<string> TrendingHashtags,
    List<string> TrendingHooks,
    List<string> SourceUrls,
    string RawOutput);

public sealed record ContentReviewDto(
    int Score,
    string HookEvaluation,
    string StorytellingEvaluation,
    string EngagementPotential,
    string CtaEffectiveness,
    string ToneReadabilityAssessment,
    List<string> GuardrailViolations,
    List<string> Recommendations,
    DateTimeOffset CreatedAtUtc);

public sealed record SchedulingBriefDto(
    string RecommendedDay,
    string RecommendedTime,
    string AudienceTimezone,
    string HashtagStrategy,
    string FirstHourEngagementPlan,
    ScheduleStatus Status,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? PublishedAtUtc,
    string? LinkedInExternalPostId);

/// <summary>Full detail for one content-generation execution — what <c>GET /api/content/{id}</c> returns.</summary>
public sealed record ContentDetailResponse(
    Guid ExecutionId,
    Guid ContentTopicId,
    string Topic,
    ExecutionStatus Status,
    int RetryCount,
    string? ErrorDetails,
    WriterPersona Persona,
    Guid? LinkedInPostId, // null until the Writer agent has produced a post; clients need it to call the Schedules endpoint
    string? DraftContent,
    string? OptimizedContent,
    PostStage? Stage,
    List<string> Hashtags,
    ResearchBriefDto? Research,
    List<ContentReviewDto> Reviews,
    ApprovalStatus? ApprovalStatus,
    SchedulingBriefDto? Scheduling,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ContentHistoryItemResponse(
    Guid ExecutionId,
    Guid CampaignId,
    string CampaignName,
    string Topic,
    ExecutionStatus Status,
    int? CritiqueScore,
    ApprovalStatus? ApprovalStatus,
    ScheduleStatus? ScheduleStatus,
    DateTimeOffset CreatedAtUtc);

/// <summary>No body needed — the reviewer is the authenticated caller (JWT), not a request field.</summary>
public sealed record ApproveContentRequest;

public sealed record RejectContentRequest(string Reason);

public sealed record EditContentRequest(string EditedContent);

public sealed record ApprovalActionResponse(Guid LinkedInPostId, ApprovalStatus Status, DateTimeOffset ReviewedAtUtc);
