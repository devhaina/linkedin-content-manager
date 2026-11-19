using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Campaigns;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Campaigns;

public sealed record GetCampaignByIdQuery(Guid Id) : IRequest<CampaignResponse>;

public sealed class GetCampaignByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCampaignByIdQuery, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns.AsNoTracking()
            .Include(c => c.Niche)
            .Include(c => c.ContentTopics)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Campaign '{request.Id}' was not found.");

        return new CampaignResponse(
            campaign.Id, campaign.Name, campaign.NicheId, campaign.Niche!.Name, campaign.DefaultPersona,
            campaign.OwnerUserId, campaign.Status, campaign.ContentTopics.Count, campaign.CreatedAtUtc, campaign.UpdatedAtUtc);
    }
}
