namespace LinkedInContentManager.Domain.Common;

/// <summary>
/// Marker for something that happened inside the Domain layer that other layers
/// may react to. Deliberately has no dependency on MediatR or any other
/// package — the Application layer adapts these into MediatR notifications
/// when dispatching them, so Domain stays dependency-free.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
