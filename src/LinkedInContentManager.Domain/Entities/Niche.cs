using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// A content niche campaigns target (Fintech, Fintech Security, Crypto, Cryptocurrency,
/// emerging AI developments). Stored as seeded, configurable data rather than a hardcoded
/// enum, per the brief, so an operator can add or retire a niche without a code change.
/// </summary>
public class Niche : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    private readonly List<Campaign> _campaigns = [];
    public IReadOnlyCollection<Campaign> Campaigns => _campaigns.AsReadOnly();

    private Niche() { }

    public Niche(string name, string slug, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name;
        Slug = string.IsNullOrWhiteSpace(slug) ? throw new ArgumentException("Slug is required.", nameof(slug)) : slug;
        Description = description ?? string.Empty;
    }

    /// <summary>Used only by deterministic seed data, where a fixed, well-known Id and audit timestamp are required.</summary>
    public Niche(Guid id, string name, string slug, string description) : this(name, slug, description)
    {
        Id = id;
        SetSeedTimestamps(SeedTimestamp);
    }

    /// <summary>Fixed instant every niche seed row is stamped with, so the EF Core model is deterministic across builds.</summary>
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
