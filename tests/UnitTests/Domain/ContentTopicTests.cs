using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.UnitTests.Domain;

public class ContentTopicTests
{
    [Fact]
    public void ResolvePersona_WithNoOverride_UsesCampaignDefault()
    {
        var topic = new ContentTopic(Guid.NewGuid(), "AI agents in fraud detection");

        topic.ResolvePersona(WriterPersona.FormalAuthoritative).Should().Be(WriterPersona.FormalAuthoritative);
    }

    [Fact]
    public void ResolvePersona_WithOverride_WinsOverCampaignDefault()
    {
        var topic = new ContentTopic(Guid.NewGuid(), "AI agents", WriterPersona.ConversationalApproachable);

        topic.ResolvePersona(WriterPersona.FormalAuthoritative).Should().Be(WriterPersona.ConversationalApproachable);
    }

    [Fact]
    public void Constructor_WithEmptyTopic_Throws()
    {
        var act = () => new ContentTopic(Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }
}
