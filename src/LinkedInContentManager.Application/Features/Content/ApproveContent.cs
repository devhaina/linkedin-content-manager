using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

public sealed record ApproveContentCommand(Guid ExecutionId, ApproveContentRequest Request) : IRequest<ApprovalActionResponse>;

public sealed class ApproveContentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ApproveContentCommand, ApprovalActionResponse>
{
    public async Task<ApprovalActionResponse> Handle(ApproveContentCommand request, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Approval)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Content execution '{request.ExecutionId}' was not found.");

        var post = execution.LinkedInPost
            ?? throw new InvalidOperationException($"Execution '{request.ExecutionId}' has no content to approve yet.");
        var approval = post.Approval
            ?? throw new InvalidOperationException($"Post '{post.Id}' has no pending approval record.");

        approval.Approve(currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);

        return new ApprovalActionResponse(post.Id, approval.Status, approval.ReviewedAtUtc!.Value);
    }
}
