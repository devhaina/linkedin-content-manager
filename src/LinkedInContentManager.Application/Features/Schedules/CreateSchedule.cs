using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Schedules;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Schedules;

public sealed record CreateScheduleCommand(CreateScheduleRequest Request) : IRequest<ScheduleResponse>;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.Request.LinkedInPostId).NotEqual(Guid.Empty);
        RuleFor(x => x.Request.ScheduledAtUtc).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class CreateScheduleCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateScheduleCommand, ScheduleResponse>
{
    public async Task<ScheduleResponse> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var post = await db.LinkedInPosts
            .Include(p => p.Approval)
            .Include(p => p.PublishingSchedule)
            .FirstOrDefaultAsync(p => p.Id == request.Request.LinkedInPostId, cancellationToken)
            ?? throw new KeyNotFoundException($"LinkedIn post '{request.Request.LinkedInPostId}' was not found.");

        if (post.Approval?.Status != ApprovalStatus.Approved)
            throw new InvalidOperationException($"Post '{post.Id}' must be approved before it can be scheduled.");

        var schedule = post.PublishingSchedule
            ?? throw new InvalidOperationException($"Post '{post.Id}' has no Scheduling Agent recommendation to commit to.");

        schedule.Schedule(request.Request.ScheduledAtUtc);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(schedule);
    }

    internal static ScheduleResponse ToResponse(Domain.Entities.PublishingSchedule s) => new(
        s.Id, s.LinkedInPostId, s.RecommendedDay, s.RecommendedTime, s.AudienceTimezone,
        s.HashtagStrategy, s.FirstHourEngagementPlan, s.Status, s.ScheduledAtUtc, s.PublishedAtUtc,
        s.LinkedInExternalPostId, s.PublishErrorDetails);
}
