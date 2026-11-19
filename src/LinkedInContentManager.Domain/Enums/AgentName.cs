namespace LinkedInContentManager.Domain.Enums;

/// <summary>The exactly-five agents in the CrewAI sequential pipeline, in execution order.</summary>
public enum AgentName
{
    TrendResearcher = 0,
    ContentWriter = 1,
    ContentCritic = 2,
    ContentOptimizer = 3,
    SchedulingAgent = 4
}
