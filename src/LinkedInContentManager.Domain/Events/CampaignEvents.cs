using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Events;

public sealed record CampaignCreatedDomainEvent(Guid CampaignId, Guid NicheId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
