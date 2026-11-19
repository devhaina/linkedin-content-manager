using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Events;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// Task 5 (Scheduling) output plus the real-world publishing lifecycle. The Scheduling
/// Agent's recommendation is non-binding (<see cref="ScheduleStatus.Recommended"/>) — it is
/// evidence-aware reasoning about typical engagement patterns, not a claim of access to real
/// analytics. <see cref="MarkPublished"/> is the ONLY way to reach
/// <see cref="ScheduleStatus.Published"/>, and it hard-fails without a real external LinkedIn
/// post ID: this is where the brief's "never claim published unless the API confirms it" rule
/// is enforced at the data layer.
/// </summary>
public class PublishingSchedule : BaseEntity
{
    public Guid LinkedInPostId { get; private set; }
    public LinkedInPost? LinkedInPost { get; private set; }

    public string RecommendedDay { get; private set; } = string.Empty;

    public string RecommendedTime { get; private set; } = string.Empty;

    /// <summary>IANA timezone id the recommendation is expressed in, e.g. "America/New_York".</summary>
    public string AudienceTimezone { get; private set; } = string.Empty;

    public string HashtagStrategy { get; private set; } = string.Empty;

    public string FirstHourEngagementPlan { get; private set; } = string.Empty;

    public ScheduleStatus Status { get; private set; } = ScheduleStatus.Recommended;

    public DateTimeOffset? ScheduledAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    /// <summary>Only ever set inside <see cref="MarkPublished"/>, from a real LinkedIn API response.</summary>
    public string? LinkedInExternalPostId { get; private set; }

    public string? PublishErrorDetails { get; private set; }

    private PublishingSchedule() { }

    public PublishingSchedule(
        Guid linkedInPostId,
        string recommendedDay,
        string recommendedTime,
        string audienceTimezone,
        string hashtagStrategy,
        string firstHourEngagementPlan)
    {
        LinkedInPostId = linkedInPostId;
        RecommendedDay = string.IsNullOrWhiteSpace(recommendedDay) ? throw new ArgumentException("RecommendedDay is required.", nameof(recommendedDay)) : recommendedDay;
        RecommendedTime = string.IsNullOrWhiteSpace(recommendedTime) ? throw new ArgumentException("RecommendedTime is required.", nameof(recommendedTime)) : recommendedTime;
        AudienceTimezone = string.IsNullOrWhiteSpace(audienceTimezone) ? throw new ArgumentException("AudienceTimezone is required.", nameof(audienceTimezone)) : audienceTimezone;
        HashtagStrategy = hashtagStrategy ?? string.Empty;
        FirstHourEngagementPlan = firstHourEngagementPlan ?? string.Empty;
    }

    public void Schedule(DateTimeOffset scheduledAtUtc)
    {
        // Scheduled -> Scheduled is a legitimate reschedule (PUT /api/schedules/{id} changing
        // the time), not a no-op transition to reject — only Published/Cancelled are terminal.
        if (Status is not (ScheduleStatus.Recommended or ScheduleStatus.Failed or ScheduleStatus.Scheduled))
            throw new InvalidStateTransitionException(nameof(PublishingSchedule), Status.ToString(), ScheduleStatus.Scheduled.ToString());

        ScheduledAtUtc = scheduledAtUtc;
        Status = ScheduleStatus.Scheduled;
        PublishErrorDetails = null;
        Touch();
    }

    /// <summary>
    /// The only path to <see cref="ScheduleStatus.Published"/>. <paramref name="linkedInExternalPostId"/>
    /// must be the real post/URN id returned by a successful LinkedIn API publish call — an empty or
    /// whitespace value means the caller does not actually have a confirmed publish, and is rejected.
    /// </summary>
    public void MarkPublished(string linkedInExternalPostId, DateTimeOffset publishedAtUtc)
    {
        if (Status != ScheduleStatus.Scheduled)
            throw new InvalidStateTransitionException(nameof(PublishingSchedule), Status.ToString(), ScheduleStatus.Published.ToString());

        if (string.IsNullOrWhiteSpace(linkedInExternalPostId))
            throw new PublishingNotConfirmedException(Id);

        LinkedInExternalPostId = linkedInExternalPostId;
        PublishedAtUtc = publishedAtUtc;
        Status = ScheduleStatus.Published;
        Touch();

        AddDomainEvent(new PostPublishedDomainEvent(LinkedInPostId, Id, linkedInExternalPostId, DateTimeOffset.UtcNow));
    }

    public void MarkFailed(string errorDetails)
    {
        if (Status != ScheduleStatus.Scheduled)
            throw new InvalidStateTransitionException(nameof(PublishingSchedule), Status.ToString(), ScheduleStatus.Failed.ToString());

        Status = ScheduleStatus.Failed;
        PublishErrorDetails = string.IsNullOrWhiteSpace(errorDetails) ? throw new ArgumentException("ErrorDetails is required.", nameof(errorDetails)) : errorDetails;
        Touch();
    }

    public void Cancel()
    {
        if (Status is ScheduleStatus.Published)
            throw new InvalidStateTransitionException(nameof(PublishingSchedule), Status.ToString(), ScheduleStatus.Cancelled.ToString());

        Status = ScheduleStatus.Cancelled;
        Touch();
    }
}
