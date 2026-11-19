using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// One <c>{topic}</c> submitted for generation under a campaign — the unit the
/// five-agent pipeline runs against. A topic may be regenerated, producing a new
/// <see cref="AgentExecution"/> each time.
/// </summary>
public class ContentTopic : BaseEntity
{
    public Guid CampaignId { get; private set; }
    public Campaign? Campaign { get; private set; }

    public string Topic { get; private set; } = string.Empty;

    /// <summary>Overrides the campaign's default persona for this topic only, when set.</summary>
    public WriterPersona? PersonaOverride { get; private set; }

    private readonly List<AgentExecution> _agentExecutions = [];
    public IReadOnlyCollection<AgentExecution> AgentExecutions => _agentExecutions.AsReadOnly();

    private ContentTopic() { }

    public ContentTopic(Guid campaignId, string topic, WriterPersona? personaOverride = null)
    {
        CampaignId = campaignId;
        Topic = string.IsNullOrWhiteSpace(topic) ? throw new ArgumentException("Topic is required.", nameof(topic)) : topic;
        PersonaOverride = personaOverride;
    }

    public WriterPersona ResolvePersona(WriterPersona campaignDefault) => PersonaOverride ?? campaignDefault;
}
