using LinkedInContentManager.Application.Features.Niches;
using LinkedInContentManager.Contracts.Niches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkedInContentManager.API.Controllers;

[ApiController]
[Route("api/niches")]
[Authorize]
public sealed class NichesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NicheResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NicheResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNichesQuery(), cancellationToken);
        return Ok(result);
    }
}
