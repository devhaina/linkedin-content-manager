using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Campaigns;

public sealed record UpdateCampaignCommand(Guid Id, UpdateCampaignRequest Request) : IRequest<CampaignResponse>;

public sealed class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.DefaultPersona).IsInEnum();
        RuleFor(x => x.Request.Status).IsInEnum();
    }
}

public sealed class UpdateCampaignCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateCampaignCommand, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns.Include(c => c.Niche).Include(c => c.ContentTopics)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Campaign '{request.Id}' was not found.");

        var req = request.Request;
        campaign.Rename(req.Name);
        campaign.ChangeDefaultPersona(req.DefaultPersona);

        switch (req.Status)
        {
            case CampaignStatus.Active: campaign.Resume(); break;
            case CampaignStatus.Paused: campaign.Pause(); break;
            case CampaignStatus.Archived: campaign.Archive(); break;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CampaignResponse(
            campaign.Id, campaign.Name, campaign.NicheId, campaign.Niche!.Name, campaign.DefaultPersona,
            campaign.OwnerUserId, campaign.Status, campaign.ContentTopics.Count, campaign.CreatedAtUtc, campaign.UpdatedAtUtc);
    }
}
