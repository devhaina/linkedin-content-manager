using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Contracts.Schedules;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Schedules;

public sealed record GetSchedulesQuery(int Page, int PageSize, ScheduleStatus? Status) : IRequest<PagedResult<ScheduleResponse>>;

public sealed class GetSchedulesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetSchedulesQuery, PagedResult<ScheduleResponse>>
{
    public async Task<PagedResult<ScheduleResponse>> Handle(GetSchedulesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.PublishingSchedules.AsNoTracking().AsQueryable();
        if (request.Status is { } status)
            query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ScheduledAtUtc ?? DateTimeOffset.MaxValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScheduleResponse(
                s.Id, s.LinkedInPostId, s.RecommendedDay, s.RecommendedTime, s.AudienceTimezone,
                s.HashtagStrategy, s.FirstHourEngagementPlan, s.Status, s.ScheduledAtUtc, s.PublishedAtUtc,
                s.LinkedInExternalPostId, s.PublishErrorDetails))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduleResponse>(items, page, pageSize, totalCount);
    }
}
