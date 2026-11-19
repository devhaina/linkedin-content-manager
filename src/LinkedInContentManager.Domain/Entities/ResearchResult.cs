using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// Structured output of Task 1 (Research), produced by the Trend Researcher agent.
/// <see cref="SourceUrls"/> is the guardrail backbone: the Critic checks every
/// statistic/claim in the draft against this list before anything ships.
/// </summary>
public class ResearchResult : BaseEntity
{
    public Guid AgentExecutionId { get; private set; }
    public AgentExecution? AgentExecution { get; private set; }

    public List<string> TrendingContentAngles { get; private set; } = [];

    public List<string> TrendingHashtags { get; private set; } = [];

    public List<string> TrendingHooks { get; private set; } = [];

    public List<string> SourceUrls { get; private set; } = [];

    /// <summary>Full unstructured brief text the agent produced, kept for audit/debugging.</summary>
    public string RawOutput { get; private set; } = string.Empty;

    private ResearchResult() { }

    public ResearchResult(
        Guid agentExecutionId,
        List<string> trendingContentAngles,
        List<string> trendingHashtags,
        List<string> trendingHooks,
        List<string> sourceUrls,
        string rawOutput)
    {
        AgentExecutionId = agentExecutionId;
        TrendingContentAngles = trendingContentAngles ?? [];
        TrendingHashtags = trendingHashtags ?? [];
        TrendingHooks = trendingHooks ?? [];
        SourceUrls = sourceUrls ?? [];
        RawOutput = rawOutput ?? string.Empty;
    }
}
