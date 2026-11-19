using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

public sealed record RejectContentCommand(Guid ExecutionId, RejectContentRequest Request) : IRequest<ApprovalActionResponse>;

public sealed class RejectContentCommandValidator : AbstractValidator<RejectContentCommand>
{
    public RejectContentCommandValidator()
    {
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class RejectContentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RejectContentCommand, ApprovalActionResponse>
{
    public async Task<ApprovalActionResponse> Handle(RejectContentCommand request, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Approval)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Content execution '{request.ExecutionId}' was not found.");

        var post = execution.LinkedInPost
            ?? throw new InvalidOperationException($"Execution '{request.ExecutionId}' has no content to reject yet.");
        var approval = post.Approval
            ?? throw new InvalidOperationException($"Post '{post.Id}' has no pending approval record.");

        approval.Reject(currentUser.UserId, request.Request.Reason);
        await db.SaveChangesAsync(cancellationToken);

        return new ApprovalActionResponse(post.Id, approval.Status, approval.ReviewedAtUtc!.Value);
    }
}
