using LinkedInContentManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Schedules;

/// <summary>"Delete" cancels the schedule (<see cref="Domain.Entities.PublishingSchedule.Cancel"/>) — it never deletes a published record.</summary>
public sealed record DeleteScheduleCommand(Guid Id) : IRequest;

public sealed class DeleteScheduleCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteScheduleCommand>
{
    public async Task Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await db.PublishingSchedules.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Publishing schedule '{request.Id}' was not found.");

        schedule.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }
}
