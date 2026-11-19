using LinkedInContentManager.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Campaigns;

/// <summary>
/// "Delete" archives rather than hard-deletes: a campaign's content/executions are an audit
/// trail (agent runs, approvals, publishing history) that must survive even once the campaign
/// itself is retired. Use <see cref="Domain.Entities.Campaign.Archive"/>, not <c>DbSet.Remove</c>.
/// </summary>
public sealed record DeleteCampaignCommand(Guid Id) : IRequest;

public sealed class DeleteCampaignCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteCampaignCommand>
{
    public async Task Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Campaign '{request.Id}' was not found.");

        campaign.Archive();
        await db.SaveChangesAsync(cancellationToken);
    }
}
