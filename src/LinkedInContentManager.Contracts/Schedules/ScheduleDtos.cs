using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Contracts.Schedules;

/// <summary>
/// Commits an already-recommended <c>PublishingSchedule</c> (produced by the Scheduling Agent)
/// to an actual send time. There is one <c>PublishingSchedule</c> row per <c>LinkedInPost</c>,
/// created automatically when that post's pipeline run completes — this endpoint moves it from
/// <see cref="ScheduleStatus.Recommended"/> to <see cref="ScheduleStatus.Scheduled"/>, it does
/// not create a new row.
/// </summary>
public sealed record CreateScheduleRequest(Guid LinkedInPostId, DateTimeOffset ScheduledAtUtc);

public sealed record UpdateScheduleRequest(DateTimeOffset ScheduledAtUtc);

public sealed record ScheduleResponse(
    Guid Id,
    Guid LinkedInPostId,
    string RecommendedDay,
    string RecommendedTime,
    string AudienceTimezone,
    string HashtagStrategy,
    string FirstHourEngagementPlan,
    ScheduleStatus Status,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? PublishedAtUtc,
    string? LinkedInExternalPostId,
    string? PublishErrorDetails);
