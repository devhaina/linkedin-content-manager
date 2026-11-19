using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Domain.Events;

public sealed record AgentExecutionStartedDomainEvent(Guid AgentExecutionId, Guid ContentTopicId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record AgentExecutionCompletedDomainEvent(Guid AgentExecutionId, ExecutionStatus FinalStatus, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record AgentExecutionFailedDomainEvent(Guid AgentExecutionId, AgentName FailedAtAgent, string ErrorDetails, DateTimeOffset OccurredAtUtc) : IDomainEvent;
