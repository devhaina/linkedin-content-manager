using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Events;

namespace LinkedInContentManager.UnitTests.Domain;

public class CampaignTests
{
    [Fact]
    public void Constructor_RaisesCampaignCreatedDomainEvent()
    {
        var campaign = new Campaign("Fintech Launch", Guid.NewGuid(), WriterPersona.Practitioner, Guid.NewGuid());

        campaign.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CampaignCreatedDomainEvent>();
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new Campaign("", Guid.NewGuid(), WriterPersona.Practitioner, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Pause_Then_Resume_RoundTrips()
    {
        var campaign = new Campaign("X", Guid.NewGuid(), WriterPersona.Practitioner, Guid.NewGuid());

        campaign.Pause();
        campaign.Status.Should().Be(CampaignStatus.Paused);

        campaign.Resume();
        campaign.Status.Should().Be(CampaignStatus.Active);
    }

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        var campaign = new Campaign("X", Guid.NewGuid(), WriterPersona.Practitioner, Guid.NewGuid());

        campaign.Archive();

        campaign.Status.Should().Be(CampaignStatus.Archived);
    }
}
