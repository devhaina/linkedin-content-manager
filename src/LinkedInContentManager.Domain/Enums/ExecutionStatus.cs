namespace LinkedInContentManager.Domain.Enums;

/// <summary>Lifecycle of one five-agent CrewAI pipeline run.</summary>
public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    PartiallyFailed = 4,
    Cancelled = 5
}
