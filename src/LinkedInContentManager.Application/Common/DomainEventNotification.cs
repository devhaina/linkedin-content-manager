using LinkedInContentManager.Domain.Common;
using MediatR;

namespace LinkedInContentManager.Application.Common;

/// <summary>
/// Adapts a Domain-layer <see cref="IDomainEvent"/> into a MediatR <see cref="INotification"/>
/// so Infrastructure's SaveChanges pipeline can dispatch it without Domain ever depending on
/// MediatR. Application-layer handlers subscribe to
/// <c>DomainEventNotification&lt;SpecificEvent&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
