using LinkedInContentManager.API.Authorization;
using LinkedInContentManager.Application.Features.Schedules;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Contracts.Schedules;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkedInContentManager.API.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public sealed class SchedulesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(ScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScheduleResponse>> Create([FromBody] CreateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateScheduleCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ScheduleResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ScheduleStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetSchedulesQuery(page, pageSize, status), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(ScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleResponse>> Update(Guid id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateScheduleCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteScheduleCommand(id), cancellationToken);
        return NoContent();
    }
}
