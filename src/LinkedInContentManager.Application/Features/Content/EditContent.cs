using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

public sealed record EditContentResponse(Guid LinkedInPostId, string OptimizedContent, int WordCount);

public sealed record EditContentCommand(Guid ExecutionId, EditContentRequest Request) : IRequest<EditContentResponse>;

public sealed class EditContentCommandValidator : AbstractValidator<EditContentCommand>
{
    public EditContentCommandValidator()
    {
        RuleFor(x => x.Request.EditedContent).NotEmpty().MaximumLength(5000);
    }
}

public sealed class EditContentCommandHandler(IApplicationDbContext db) : IRequestHandler<EditContentCommand, EditContentResponse>
{
    public async Task<EditContentResponse> Handle(EditContentCommand request, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions
            .Include(e => e.LinkedInPost)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Content execution '{request.ExecutionId}' was not found.");

        var post = execution.LinkedInPost
            ?? throw new InvalidOperationException($"Execution '{request.ExecutionId}' has no content to edit yet.");

        post.EditOptimizedContent(request.Request.EditedContent);
        await db.SaveChangesAsync(cancellationToken);

        return new EditContentResponse(post.Id, post.OptimizedContent!, post.OptimizedWordCount!.Value);
    }
}
