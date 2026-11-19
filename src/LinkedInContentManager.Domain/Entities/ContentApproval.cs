using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;
using LinkedInContentManager.Domain.Events;
using LinkedInContentManager.Domain.Exceptions;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>The human approval gate a <see cref="LinkedInPost"/> must pass before it becomes eligible for scheduling/publishing.</summary>
public class ContentApproval : BaseEntity
{
    public Guid LinkedInPostId { get; private set; }
    public LinkedInPost? LinkedInPost { get; private set; }

    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public string? RejectionReason { get; private set; }

    private ContentApproval() { }

    public ContentApproval(Guid linkedInPostId)
    {
        LinkedInPostId = linkedInPostId;
    }

    public void Approve(Guid reviewedByUserId)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidStateTransitionException(nameof(ContentApproval), Status.ToString(), ApprovalStatus.Approved.ToString());

        Status = ApprovalStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = null;
        Touch();

        AddDomainEvent(new ContentApprovedDomainEvent(LinkedInPostId, reviewedByUserId, DateTimeOffset.UtcNow));
    }

    public void Reject(Guid reviewedByUserId, string reason)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidStateTransitionException(nameof(ContentApproval), Status.ToString(), ApprovalStatus.Rejected.ToString());

        Status = ApprovalStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("Reason is required.", nameof(reason)) : reason;
        Touch();

        AddDomainEvent(new ContentRejectedDomainEvent(LinkedInPostId, reviewedByUserId, RejectionReason, DateTimeOffset.UtcNow));
    }
}
