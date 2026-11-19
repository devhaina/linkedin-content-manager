using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Events;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// A content campaign: one niche, one default writer persona, owned by a user.
/// Holds the <see cref="ContentTopic"/>s that get run through the five-agent pipeline.
/// </summary>
public class Campaign : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public Guid NicheId { get; private set; }
    public Niche? Niche { get; private set; }

    public WriterPersona DefaultPersona { get; private set; }

    public Guid OwnerUserId { get; private set; }
    public User? OwnerUser { get; private set; }

    public CampaignStatus Status { get; private set; } = CampaignStatus.Active;

    private readonly List<ContentTopic> _contentTopics = [];
    public IReadOnlyCollection<ContentTopic> ContentTopics => _contentTopics.AsReadOnly();

    private Campaign() { }

    public Campaign(string name, Guid nicheId, WriterPersona defaultPersona, Guid ownerUserId)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name;
        NicheId = nicheId;
        DefaultPersona = defaultPersona;
        OwnerUserId = ownerUserId;

        AddDomainEvent(new CampaignCreatedDomainEvent(Id, NicheId, DateTimeOffset.UtcNow));
    }

    public void Rename(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name;
        Touch();
    }

    public void ChangeDefaultPersona(WriterPersona persona)
    {
        DefaultPersona = persona;
        Touch();
    }

    public void Pause()
    {
        Status = CampaignStatus.Paused;
        Touch();
    }

    public void Resume()
    {
        Status = CampaignStatus.Active;
        Touch();
    }

    public void Archive()
    {
        Status = CampaignStatus.Archived;
        Touch();
    }
}
