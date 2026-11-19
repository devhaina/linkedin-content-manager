using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Contracts.Niches;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Features.Niches;

/// <summary>
/// Lists the active, seeded niches — not in the brief's original endpoint list, but required
/// for any real client (the Angular dashboard included) to populate a campaign's niche picker
/// without hardcoding GUIDs. Niches are seeded data (see <c>NicheSeedData</c>), not an enum, so
/// this is the only way a client can discover them.
/// </summary>
public sealed record GetNichesQuery : IRequest<IReadOnlyList<NicheResponse>>;

public sealed class GetNichesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetNichesQuery, IReadOnlyList<NicheResponse>>
{
    public async Task<IReadOnlyList<NicheResponse>> Handle(GetNichesQuery request, CancellationToken cancellationToken) =>
        await db.Niches.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.Name)
            .Select(n => new NicheResponse(n.Id, n.Name, n.Slug, n.Description))
            .ToListAsync(cancellationToken);
}
