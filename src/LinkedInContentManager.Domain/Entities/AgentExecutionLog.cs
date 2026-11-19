using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>One agent/task step within an <see cref="AgentExecution"/> — the audit trail CloudWatch-style dashboards read.</summary>
public class AgentExecutionLog : BaseEntity
{
    public Guid AgentExecutionId { get; private set; }
    public AgentExecution? AgentExecution { get; private set; }

    public AgentName Agent { get; private set; }

    public string TaskName { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public long DurationMs { get; private set; }

    public int? TokenUsage { get; private set; }

    private AgentExecutionLog() { }

    internal AgentExecutionLog(Guid agentExecutionId, AgentName agent, string taskName, string message, long durationMs, int? tokenUsage)
    {
        AgentExecutionId = agentExecutionId;
        Agent = agent;
        TaskName = string.IsNullOrWhiteSpace(taskName) ? throw new ArgumentException("TaskName is required.", nameof(taskName)) : taskName;
        Message = message ?? string.Empty;
        DurationMs = durationMs;
        TokenUsage = tokenUsage;
    }
}
