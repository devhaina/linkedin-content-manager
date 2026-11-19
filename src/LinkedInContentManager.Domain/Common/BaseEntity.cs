using System.ComponentModel.DataAnnotations.Schema;

namespace LinkedInContentManager.Domain.Common;

/// <summary>
/// Base for every entity: a surrogate key plus the audit fields required
/// across the whole schema (CreatedAt/UpdatedAt) and a domain-event outbox
/// that EF Core's SaveChanges interceptor drains and dispatches.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Events raised by this entity since it was loaded, not yet dispatched.</summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Used only by deterministic EF Core seed data (<c>HasData</c>), where
    /// <see cref="CreatedAtUtc"/>/<see cref="UpdatedAtUtc"/> must be a fixed value instead of
    /// "now" — otherwise the model is non-deterministic and every design-time build looks like
    /// a pending migration.
    /// </summary>
    protected void SetSeedTimestamps(DateTimeOffset at)
    {
        CreatedAtUtc = at;
        UpdatedAtUtc = at;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Called by the persistence layer whenever the entity is mutated and saved.</summary>
    public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
