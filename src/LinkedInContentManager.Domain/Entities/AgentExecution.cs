using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Events;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// One run of the five-agent sequential CrewAI pipeline (Research → Writing → Critique →
/// Optimization → Scheduling) against a single <see cref="ContentTopic"/>. Tracks status,
/// retry count, correlation with the CrewAI microservice, and error detail for production
/// diagnostics without ever exposing secrets.
/// </summary>
public class AgentExecution : BaseEntity
{
    public Guid ContentTopicId { get; private set; }
    public ContentTopic? ContentTopic { get; private set; }

    /// <summary>Correlation ID propagated to the CrewAI HTTP client and into Serilog/OpenTelemetry.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>The execution ID returned by the CrewAI FastAPI service once the job is accepted.</summary>
    public string? ExternalExecutionId { get; private set; }

    public ExecutionStatus Status { get; private set; } = ExecutionStatus.Pending;

    public int RetryCount { get; private set; }

    public string? ErrorDetails { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public ResearchResult? ResearchResult { get; private set; }

    public LinkedInPost? LinkedInPost { get; private set; }

    private readonly List<AgentExecutionLog> _logs = [];
    public IReadOnlyCollection<AgentExecutionLog> Logs => _logs.AsReadOnly();

    private AgentExecution() { }

    public AgentExecution(Guid contentTopicId, string correlationId)
    {
        ContentTopicId = contentTopicId;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? throw new ArgumentException("CorrelationId is required.", nameof(correlationId))
            : correlationId;
    }

    /// <summary>
    /// Marks the execution Running the moment the Worker picks the job off the queue — BEFORE
    /// the (potentially minutes-long) CrewAI call is made. Callers must save this transition
    /// immediately rather than waiting for the external call to return, so
    /// <c>GET /api/content/{id}</c> reflects "actually in progress" instead of sitting on
    /// "Pending" for the whole duration of a long-running external call.
    /// </summary>
    public void Start()
    {
        // Pending (normal first run), Failed/PartiallyFailed (either can legitimately be
        // retried — PartiallyFailed is what a first-time Fail() produces, so excluding it here
        // while RegisterRetry() resets to Pending anyway would make this guard only accidentally
        // correct in practice).
        if (Status is not (ExecutionStatus.Pending or ExecutionStatus.Failed or ExecutionStatus.PartiallyFailed))
            throw new InvalidStateTransitionException(nameof(AgentExecution), Status.ToString(), ExecutionStatus.Running.ToString());

        Status = ExecutionStatus.Running;
        StartedAtUtc = DateTimeOffset.UtcNow;
        ErrorDetails = null;
        Touch();

        AddDomainEvent(new AgentExecutionStartedDomainEvent(Id, ContentTopicId, DateTimeOffset.UtcNow));
    }

    /// <summary>Records the execution ID the CrewAI service assigned, once its (slow) response actually arrives.</summary>
    public void SetExternalExecutionId(string externalExecutionId)
    {
        ExternalExecutionId = string.IsNullOrWhiteSpace(externalExecutionId)
            ? throw new ArgumentException("ExternalExecutionId is required.", nameof(externalExecutionId))
            : externalExecutionId;
        Touch();
    }

    public AgentExecutionLog RecordLog(AgentName agent, string taskName, string message, long durationMs, int? tokenUsage = null)
    {
        var log = new AgentExecutionLog(Id, agent, taskName, message, durationMs, tokenUsage);
        _logs.Add(log);
        Touch();
        return log;
    }

    public void Complete()
    {
        Status = ExecutionStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();

        AddDomainEvent(new AgentExecutionCompletedDomainEvent(Id, Status, DateTimeOffset.UtcNow));
    }

    public void Fail(AgentName failedAtAgent, string errorDetails)
    {
        Status = RetryCount > 0 ? ExecutionStatus.Failed : ExecutionStatus.PartiallyFailed;
        ErrorDetails = errorDetails;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();

        AddDomainEvent(new AgentExecutionFailedDomainEvent(Id, failedAtAgent, errorDetails, DateTimeOffset.UtcNow));
    }

    public void RegisterRetry()
    {
        RetryCount++;
        Status = ExecutionStatus.Pending;
        Touch();
    }

    public void AttachResearchResult(ResearchResult researchResult)
    {
        if (ResearchResult is not null)
            throw new InvalidStateTransitionException(nameof(AgentExecution), "ResearchResult already set", "new ResearchResult");
        ResearchResult = researchResult;
        Touch();
    }

    public void AttachLinkedInPost(LinkedInPost post)
    {
        if (LinkedInPost is not null)
            throw new InvalidStateTransitionException(nameof(AgentExecution), "LinkedInPost already set", "new LinkedInPost");
        LinkedInPost = post;
        Touch();
    }
}
