using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

/// <summary>Re-runs the pipeline for an existing <see cref="ContentTopic"/> as a brand-new <see cref="AgentExecution"/> — it never mutates the original one.</summary>
public sealed record RegenerateContentCommand(Guid ExecutionId, RegenerateContentRequest Request) : IRequest<GenerateContentResponse>;

public sealed class RegenerateContentCommandHandler(IApplicationDbContext db, IContentGenerationJobPublisher publisher)
    : IRequestHandler<RegenerateContentCommand, GenerateContentResponse>
{
    public async Task<GenerateContentResponse> Handle(RegenerateContentCommand request, CancellationToken cancellationToken)
    {
        var priorExecution = await db.AgentExecutions
            .Include(e => e.ContentTopic!).ThenInclude(t => t!.Campaign!).ThenInclude(c => c!.Niche)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Content execution '{request.ExecutionId}' was not found.");

        var topic = priorExecution.ContentTopic!;
        var campaign = topic.Campaign!;

        var correlationId = Guid.NewGuid().ToString();
        var execution = new AgentExecution(topic.Id, correlationId);
        db.AgentExecutions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);

        var persona = request.Request.PersonaOverride ?? topic.ResolvePersona(campaign.DefaultPersona);
        await publisher.PublishAsync(
            new ExternalServices.Messaging.ContentGenerationJobMessage(execution.Id, topic.Id, topic.Topic, campaign.Niche!.Name, persona, correlationId),
            cancellationToken);

        return new GenerateContentResponse(execution.Id, execution.Status);
    }
}
