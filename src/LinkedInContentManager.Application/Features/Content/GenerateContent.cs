using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Application.ExternalServices.Messaging;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

/// <summary>
/// Creates the <see cref="ContentTopic"/> + <see cref="AgentExecution"/> rows (status
/// <see cref="ExecutionStatus.Pending"/>) and enqueues the job — it does NOT call the CrewAI
/// service itself. The Worker (Phase 5's RabbitMQ consumer) does that asynchronously, because
/// a real 5-agent run takes 1-3 minutes and must not block an HTTP request.
/// </summary>
public sealed record GenerateContentCommand(GenerateContentRequest Request) : IRequest<GenerateContentResponse>;

public sealed class GenerateContentCommandValidator : AbstractValidator<GenerateContentCommand>
{
    public GenerateContentCommandValidator()
    {
        RuleFor(x => x.Request.CampaignId).NotEqual(Guid.Empty);
        RuleFor(x => x.Request.Topic).NotEmpty().MinimumLength(3).MaximumLength(500);
        RuleFor(x => x.Request.PersonaOverride!).IsInEnum().When(x => x.Request.PersonaOverride.HasValue);
    }
}

public sealed class GenerateContentCommandHandler(IApplicationDbContext db, IContentGenerationJobPublisher publisher)
    : IRequestHandler<GenerateContentCommand, GenerateContentResponse>
{
    public async Task<GenerateContentResponse> Handle(GenerateContentCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var campaign = await db.Campaigns.Include(c => c.Niche)
            .FirstOrDefaultAsync(c => c.Id == req.CampaignId, cancellationToken)
            ?? throw new KeyNotFoundException($"Campaign '{req.CampaignId}' was not found.");

        if (campaign.Status != CampaignStatus.Active)
            throw new InvalidOperationException($"Campaign '{campaign.Id}' is {campaign.Status} and cannot generate new content.");

        var topic = new ContentTopic(campaign.Id, req.Topic, req.PersonaOverride);
        db.ContentTopics.Add(topic);

        var correlationId = Guid.NewGuid().ToString();
        var execution = new AgentExecution(topic.Id, correlationId);
        db.AgentExecutions.Add(execution);

        await db.SaveChangesAsync(cancellationToken);

        var persona = topic.ResolvePersona(campaign.DefaultPersona);
        await publisher.PublishAsync(
            new ContentGenerationJobMessage(execution.Id, topic.Id, req.Topic, campaign.Niche!.Name, persona, correlationId),
            cancellationToken);

        return new GenerateContentResponse(execution.Id, execution.Status);
    }
}
