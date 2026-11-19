using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Events;

public sealed record ContentApprovedDomainEvent(Guid LinkedInPostId, Guid ApprovedByUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ContentRejectedDomainEvent(Guid LinkedInPostId, Guid RejectedByUserId, string Reason, DateTimeOffset OccurredAtUtc) : IDomainEvent;

/// <summary>
/// Raised only by <c>PublishingSchedule.MarkPublished</c>, which requires a real,
/// non-empty external LinkedIn post ID. This event is the system's only source of
/// truth that a post actually went live — nothing upstream may assume publication
/// happened without this event having fired.
/// </summary>
public sealed record PostPublishedDomainEvent(Guid LinkedInPostId, Guid PublishingScheduleId, string LinkedInExternalPostId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
