namespace LinkedInContentManager.Domain.Exceptions;

/// <summary>Base for every exception raised by a business-rule violation inside the Domain layer.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>Raised when an entity is asked to transition into a state its current state does not allow.</summary>
public sealed class InvalidStateTransitionException(string entityName, string fromState, string toState)
    : DomainException($"{entityName} cannot transition from '{fromState}' to '{toState}'.");

/// <summary>
/// Raised when code tries to mark a <c>PublishingSchedule</c> as published without a real,
/// non-empty external confirmation from the LinkedIn API. Publishing must never be faked.
/// </summary>
public sealed class PublishingNotConfirmedException(Guid publishingScheduleId)
    : DomainException($"PublishingSchedule {publishingScheduleId} cannot be marked Published without a non-empty LinkedIn external post ID confirming the API call succeeded.");

/// <summary>Raised when a content-guardrail violation (unverified claim, competitor mention, political topic) blocks a transition.</summary>
public sealed class ContentGuardrailViolationException(string reason)
    : DomainException($"Content guardrail violation: {reason}");
