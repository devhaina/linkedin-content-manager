using FluentValidation;
using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Schedules;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Schedules;

/// <summary>Re-schedules a schedule that is <see cref="ScheduleStatus.Scheduled"/> or <see cref="ScheduleStatus.Failed"/> to a new time.</summary>
public sealed record UpdateScheduleCommand(Guid Id, UpdateScheduleRequest Request) : IRequest<ScheduleResponse>;

public sealed class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleCommandValidator()
    {
        RuleFor(x => x.Request.ScheduledAtUtc).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class UpdateScheduleCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateScheduleCommand, ScheduleResponse>
{
    public async Task<ScheduleResponse> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await db.PublishingSchedules.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Publishing schedule '{request.Id}' was not found.");

        schedule.Schedule(request.Request.ScheduledAtUtc);
        await db.SaveChangesAsync(cancellationToken);

        return CreateScheduleCommandHandler.ToResponse(schedule);
    }
}
