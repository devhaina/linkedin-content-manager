using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Campaigns;

public sealed record CreateCampaignCommand(CreateCampaignRequest Request) : IRequest<CampaignResponse>;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.NicheId).NotEqual(Guid.Empty);
        RuleFor(x => x.Request.DefaultPersona).IsInEnum();
    }
}

public sealed class CreateCampaignCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateCampaignCommand, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var niche = await db.Niches.FirstOrDefaultAsync(n => n.Id == req.NicheId && n.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Active niche '{req.NicheId}' was not found.");

        var campaign = new Campaign(req.Name, req.NicheId, req.DefaultPersona, currentUser.UserId);

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        return new CampaignResponse(
            campaign.Id, campaign.Name, niche.Id, niche.Name, campaign.DefaultPersona,
            campaign.OwnerUserId, campaign.Status, 0, campaign.CreatedAtUtc, campaign.UpdatedAtUtc);
    }
}
