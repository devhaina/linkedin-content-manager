using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Content;

public sealed record GetContentByIdQuery(Guid ExecutionId) : IRequest<ContentDetailResponse>;

public sealed class GetContentByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetContentByIdQuery, ContentDetailResponse>
{
    public async Task<ContentDetailResponse> Handle(GetContentByIdQuery request, CancellationToken cancellationToken)
    {
        var execution = await db.AgentExecutions.AsNoTracking()
            .Include(e => e.ContentTopic)
            .Include(e => e.ResearchResult)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Hashtags).ThenInclude(h => h.Hashtag)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Reviews)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.Approval)
            .Include(e => e.LinkedInPost!).ThenInclude(p => p.PublishingSchedule)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Content execution '{request.ExecutionId}' was not found.");

        var post = execution.LinkedInPost;
        var research = execution.ResearchResult;

        return new ContentDetailResponse(
            execution.Id,
            execution.ContentTopicId,
            execution.ContentTopic!.Topic,
            execution.Status,
            execution.RetryCount,
            execution.ErrorDetails,
            post?.Persona ?? execution.ContentTopic.PersonaOverride ?? default,
            post?.Id,
            post?.DraftContent,
            post?.OptimizedContent,
            post?.Stage,
            post?.Hashtags.OrderBy(h => h.Position).Select(h => h.Hashtag!.Text).ToList() ?? [],
            research is null ? null : new ResearchBriefDto(
                research.TrendingContentAngles, research.TrendingHashtags, research.TrendingHooks,
                research.SourceUrls, research.RawOutput),
            post?.Reviews.OrderBy(r => r.CreatedAtUtc).Select(r => new ContentReviewDto(
                r.Score, r.HookEvaluation, r.StorytellingEvaluation, r.EngagementPotential,
                r.CtaEffectiveness, r.ToneReadabilityAssessment, r.GuardrailViolations, r.Recommendations, r.CreatedAtUtc)).ToList()
                ?? [],
            post?.Approval?.Status,
            post?.PublishingSchedule is null ? null : new SchedulingBriefDto(
                post.PublishingSchedule.RecommendedDay, post.PublishingSchedule.RecommendedTime,
                post.PublishingSchedule.AudienceTimezone, post.PublishingSchedule.HashtagStrategy,
                post.PublishingSchedule.FirstHourEngagementPlan, post.PublishingSchedule.Status,
                post.PublishingSchedule.ScheduledAtUtc, post.PublishingSchedule.PublishedAtUtc,
                post.PublishingSchedule.LinkedInExternalPostId),
            execution.CreatedAtUtc,
            execution.UpdatedAtUtc);
    }
}
