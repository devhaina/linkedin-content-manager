using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.UnitTests.Domain;

/// <summary>
/// Locks in the brief's single highest-priority rule: never claim a post was published unless
/// the external LinkedIn API actually confirmed it. <see cref="PublishingSchedule.MarkPublished"/>
/// is the only path to <see cref="ScheduleStatus.Published"/>, and every way to reach it without
/// a real external post ID must fail.
/// </summary>
public class PublishingScheduleTests
{
    private static PublishingSchedule CreateRecommended() => new(
        Guid.NewGuid(), "Tuesday", "10:00 AM", "America/New_York", "use niche hashtags", "reply to comments");

    [Fact]
    public void MarkPublished_WithEmptyExternalId_Throws()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        var act = () => schedule.MarkPublished("", DateTimeOffset.UtcNow);

        act.Should().Throw<PublishingNotConfirmedException>();
        schedule.Status.Should().Be(ScheduleStatus.Scheduled);
    }

    [Fact]
    public void MarkPublished_WithWhitespaceExternalId_Throws()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        var act = () => schedule.MarkPublished("   ", DateTimeOffset.UtcNow);

        act.Should().Throw<PublishingNotConfirmedException>();
    }

    [Fact]
    public void MarkPublished_WithoutFirstBeingScheduled_Throws()
    {
        var schedule = CreateRecommended(); // still Recommended, never Scheduled

        var act = () => schedule.MarkPublished("urn:li:share:123", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void MarkPublished_WithRealExternalId_Succeeds()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        schedule.MarkPublished("urn:li:share:7123456789", DateTimeOffset.UtcNow);

        schedule.Status.Should().Be(ScheduleStatus.Published);
        schedule.LinkedInExternalPostId.Should().Be("urn:li:share:7123456789");
        schedule.PublishedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Schedule_FromRecommended_Succeeds()
    {
        var schedule = CreateRecommended();

        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        schedule.Status.Should().Be(ScheduleStatus.Scheduled);
    }

    [Fact]
    public void Schedule_Reschedule_FromAlreadyScheduled_Succeeds()
    {
        // Regression test for the real bug Phase 5 live testing caught: PUT /api/schedules/{id}
        // (changing an already-Scheduled time) must not be treated as an invalid transition.
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        var act = () => schedule.Schedule(DateTimeOffset.UtcNow.AddDays(3));

        act.Should().NotThrow();
        schedule.Status.Should().Be(ScheduleStatus.Scheduled);
    }

    [Fact]
    public void Schedule_AfterPublished_Throws()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));
        schedule.MarkPublished("urn:li:share:1", DateTimeOffset.UtcNow);

        var act = () => schedule.Schedule(DateTimeOffset.UtcNow.AddDays(5));

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cancel_AfterPublished_Throws()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));
        schedule.MarkPublished("urn:li:share:1", DateTimeOffset.UtcNow);

        var act = schedule.Cancel;

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cancel_WhileRecommended_Succeeds()
    {
        var schedule = CreateRecommended();

        schedule.Cancel();

        schedule.Status.Should().Be(ScheduleStatus.Cancelled);
    }

    [Fact]
    public void MarkFailed_RequiresNonEmptyErrorDetails()
    {
        var schedule = CreateRecommended();
        schedule.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        var act = () => schedule.MarkFailed("");

        act.Should().Throw<ArgumentException>();
    }
}
