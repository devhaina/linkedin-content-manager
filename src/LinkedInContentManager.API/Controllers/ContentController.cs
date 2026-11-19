using LinkedInContentManager.API.Authorization;
using LinkedInContentManager.Application.Features.Content;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkedInContentManager.API.Controllers;

[ApiController]
[Route("api/content")]
[Authorize]
public sealed class ContentController(IMediator mediator) : ControllerBase
{
    [HttpPost("generate")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(GenerateContentResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<GenerateContentResponse>> Generate([FromBody] GenerateContentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GenerateContentCommand(request), cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = result.ExecutionId }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetContentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<ContentHistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ContentHistoryItemResponse>>> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? campaignId = null, [FromQuery] ExecutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetContentHistoryQuery(page, pageSize, campaignId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/regenerate")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(GenerateContentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateContentResponse>> Regenerate(Guid id, [FromBody] RegenerateContentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegenerateContentCommand(id, request), cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = result.ExecutionId }, result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(ApprovalActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApprovalActionResponse>> Approve(Guid id, [FromBody] ApproveContentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveContentCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(ApprovalActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApprovalActionResponse>> Reject(Guid id, [FromBody] RejectContentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejectContentCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/edit")]
    [Authorize(Roles = Roles.Editors)]
    [ProducesResponseType(typeof(EditContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EditContentResponse>> Edit(Guid id, [FromBody] EditContentRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EditContentCommand(id, request), cancellationToken);
        return Ok(result);
    }
}
