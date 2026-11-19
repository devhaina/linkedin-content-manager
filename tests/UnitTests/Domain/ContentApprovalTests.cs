using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.UnitTests.Domain;

public class ContentApprovalTests
{
    [Fact]
    public void Approve_FromPending_Succeeds()
    {
        var approval = new ContentApproval(Guid.NewGuid());
        var reviewer = Guid.NewGuid();

        approval.Approve(reviewer);

        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.ReviewedByUserId.Should().Be(reviewer);
        approval.ReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_Twice_Throws()
    {
        var approval = new ContentApproval(Guid.NewGuid());
        approval.Approve(Guid.NewGuid());

        var act = () => approval.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Reject_AfterApprove_Throws()
    {
        // Regression test for the real behavior confirmed live in Phase 5: a second review
        // action on an already-decided approval must 409, not silently overwrite the decision.
        var approval = new ContentApproval(Guid.NewGuid());
        approval.Approve(Guid.NewGuid());

        var act = () => approval.Reject(Guid.NewGuid(), "too late");

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var approval = new ContentApproval(Guid.NewGuid());

        var act = () => approval.Reject(Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_FromPending_Succeeds()
    {
        var approval = new ContentApproval(Guid.NewGuid());

        approval.Reject(Guid.NewGuid(), "needs a stronger CTA");

        approval.Status.Should().Be(ApprovalStatus.Rejected);
        approval.RejectionReason.Should().Be("needs a stronger CTA");
    }
}
