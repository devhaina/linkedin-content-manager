using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

public sealed record GetContentHistoryQuery(int Page, int PageSize, Guid? CampaignId, ExecutionStatus? Status)
    : IRequest<PagedResult<ContentHistoryItemResponse>>;

public sealed class GetContentHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContentHistoryQuery, PagedResult<ContentHistoryItemResponse>>
{
    public async Task<PagedResult<ContentHistoryItemResponse>> Handle(GetContentHistoryQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.AgentExecutions.AsNoTracking()
            .Include(e => e.ContentTopic!).ThenInclude(t => t!.Campaign)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Reviews)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Approval)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.PublishingSchedule)
            .AsQueryable();

        if (request.CampaignId is { } campaignId)
            query = query.Where(e => e.ContentTopic!.CampaignId == campaignId);
        if (request.Status is { } status)
            query = query.Where(e => e.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var executions = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = executions.Select(e => new ContentHistoryItemResponse(
            e.Id,
            e.ContentTopic!.CampaignId,
            e.ContentTopic.Campaign!.Name,
            e.ContentTopic.Topic,
            e.Status,
            e.LinkedInPost?.Reviews.OrderByDescending(r => r.CreatedAtUtc).FirstOrDefault()?.Score,
            e.LinkedInPost?.Approval?.Status,
            e.LinkedInPost?.PublishingSchedule?.Status,
            e.CreatedAtUtc)).ToList();

        return new PagedResult<ContentHistoryItemResponse>(items, page, pageSize, totalCount);
    }
}
