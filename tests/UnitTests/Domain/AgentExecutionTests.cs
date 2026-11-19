using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.UnitTests.Domain;

public class AgentExecutionTests
{
    private static AgentExecution CreatePending() => new(Guid.NewGuid(), Guid.NewGuid().ToString());

    [Fact]
    public void Start_FromPending_TransitionsToRunning()
    {
        var execution = CreatePending();

        execution.Start();

        execution.Status.Should().Be(ExecutionStatus.Running);
        execution.StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_Throws()
    {
        var execution = CreatePending();
        execution.Start();

        var act = () => execution.Start();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Start_AfterFailed_IsAllowed_ForRetry()
    {
        var execution = CreatePending();
        execution.Start();
        execution.Fail(AgentName.ContentWriter, "boom");

        var act = () => execution.Start();

        act.Should().NotThrow();
        execution.Status.Should().Be(ExecutionStatus.Running);
    }

    [Fact]
    public void SetExternalExecutionId_WithEmptyValue_Throws()
    {
        var execution = CreatePending();
        execution.Start();

        var act = () => execution.SetExternalExecutionId("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_SetsStatusAndCompletedAt()
    {
        var execution = CreatePending();
        execution.Start();

        execution.Complete();

        execution.Status.Should().Be(ExecutionStatus.Completed);
        execution.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Fail_FirstAttempt_IsPartiallyFailed_NotTerminalFailed()
    {
        var execution = CreatePending();
        execution.Start();

        execution.Fail(AgentName.ContentCritic, "LLM timeout");

        execution.Status.Should().Be(ExecutionStatus.PartiallyFailed);
        execution.ErrorDetails.Should().Be("LLM timeout");
    }

    [Fact]
    public void RegisterRetry_IncrementsCountAndResetsToPending()
    {
        var execution = CreatePending();
        execution.Start();
        execution.Fail(AgentName.ContentCritic, "boom");

        execution.RegisterRetry();

        execution.RetryCount.Should().Be(1);
        execution.Status.Should().Be(ExecutionStatus.Pending);
    }

    [Fact]
    public void Fail_AfterARetry_IsTerminalFailed()
    {
        var execution = CreatePending();
        execution.Start();
        execution.Fail(AgentName.ContentCritic, "first failure");
        execution.RegisterRetry();
        execution.Start();

        execution.Fail(AgentName.ContentCritic, "second failure");

        execution.Status.Should().Be(ExecutionStatus.Failed);
    }

    [Fact]
    public void AttachResearchResult_Twice_Throws()
    {
        var execution = CreatePending();
        var research = new ResearchResult(execution.Id, [], [], [], [], "brief");
        execution.AttachResearchResult(research);

        var act = () => execution.AttachResearchResult(research);

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void RecordLog_AddsToLogsCollection()
    {
        var execution = CreatePending();

        var log = execution.RecordLog(AgentName.TrendResearcher, "Research Task", "done", 1234, tokenUsage: 500);

        execution.Logs.Should().ContainSingle().Which.Should().BeSameAs(log);
        log.DurationMs.Should().Be(1234);
    }
}
