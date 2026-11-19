using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Contracts.Campaigns;

/// <summary>OwnerUserId is no longer part of the request — the authenticated caller (JWT) owns the campaign they create.</summary>
public sealed record CreateCampaignRequest(string Name, Guid NicheId, WriterPersona DefaultPersona);

public sealed record UpdateCampaignRequest(string Name, WriterPersona DefaultPersona, CampaignStatus Status);

public sealed record CampaignResponse(
    Guid Id,
    string Name,
    Guid NicheId,
    string NicheName,
    WriterPersona DefaultPersona,
    Guid OwnerUserId,
    CampaignStatus Status,
    int ContentTopicCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
