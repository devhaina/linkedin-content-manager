using LinkedInContentManager.API.Authorization;
using LinkedInContentManager.Application.Features.Campaigns;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkedInContentManager.API.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
public sealed class CampaignsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CampaignResponse>> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateCampaignCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CampaignResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CampaignResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? nicheId = null, [FromQuery] CampaignStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCampaignsQuery(page, pageSize, nicheId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCampaignByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(CampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignResponse>> Update(Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateCampaignCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCampaignCommand(id), cancellationToken);
        return NoContent();
    }
}
