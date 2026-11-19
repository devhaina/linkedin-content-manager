using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Campaigns;

public sealed record GetCampaignsQuery(int Page, int PageSize, Guid? NicheId, CampaignStatus? Status)
    : IRequest<PagedResult<CampaignResponse>>;

public sealed class GetCampaignsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCampaignsQuery, PagedResult<CampaignResponse>>
{
    public async Task<PagedResult<CampaignResponse>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Campaigns.AsNoTracking().Include(c => c.Niche).AsQueryable();

        if (request.NicheId is { } nicheId)
            query = query.Where(c => c.NicheId == nicheId);
        if (request.Status is { } status)
            query = query.Where(c => c.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignResponse(
                c.Id, c.Name, c.NicheId, c.Niche!.Name, c.DefaultPersona,
                c.OwnerUserId, c.Status, c.ContentTopics.Count, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CampaignResponse>(items, page, pageSize, totalCount);
    }
}
