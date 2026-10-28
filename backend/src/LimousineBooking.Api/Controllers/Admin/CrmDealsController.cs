using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only CRM deal (sales opportunity) management, including the
/// Kanban board endpoint the frontend's drag-and-drop calls. A deal never
/// creates a booking automatically, even when moved into a Won stage.
/// </summary>
[ApiController]
[Route("api/admin/crm/deals")]
[Authorize(Roles = "Administrator")]
public class CrmDealsController : ControllerBase
{
    private readonly ICrmDealService _dealService;

    public CrmDealsController(ICrmDealService dealService)
    {
        _dealService = dealService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmDealResponse>>> GetAll([FromQuery] CrmDealSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _dealService.SearchAsync(query, cancellationToken));

    /// <summary>All open deals in the default pipeline, grouped by stage — backs the Kanban board.</summary>
    /// <response code="404">No default pipeline is configured.</response>
    [HttpGet("board")]
    public async Task<ActionResult<CrmPipelineBoardResponse>> GetBoard(CancellationToken cancellationToken)
    {
        var board = await _dealService.GetBoardAsync(cancellationToken);
        return board is null ? NotFound(new { message = "No default pipeline is configured." }) : Ok(board);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CrmDealDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _dealService.GetDetailByIdAsync(id, cancellationToken);
        return detail is null ? NotFound(new { message = "Deal not found." }) : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<CrmDealResponse>> Create([FromBody] CreateCrmDealRequest request, CancellationToken cancellationToken)
    {
        var result = await _dealService.CreateAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Deal!.Id }, result.Deal)
            : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CrmDealResponse>> Update(Guid id, [FromBody] UpdateCrmDealRequest request, CancellationToken cancellationToken)
    {
        var result = await _dealService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    /// <summary>Moves the deal to a different stage — the Kanban board's drag-and-drop endpoint. Requires a loss reason when the target stage is flagged Lost.</summary>
    [HttpPut("{id:guid}/stage")]
    public async Task<ActionResult<CrmDealResponse>> MoveStage(Guid id, [FromBody] MoveCrmDealStageRequest request, CancellationToken cancellationToken)
    {
        var result = await _dealService.MoveStageAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<CrmDealResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dealService.SetArchivedAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    [HttpPut("{id:guid}/unarchive")]
    public async Task<ActionResult<CrmDealResponse>> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dealService.SetArchivedAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<CrmDealResponse>> LinkContact(Guid id, [FromBody] LinkCrmDealContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _dealService.LinkContactAsync(id, request.ContactId, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<CrmDealResponse>> UnlinkContact(Guid id, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _dealService.UnlinkContactAsync(id, contactId, cancellationToken);
        return result.Succeeded ? Ok(result.Deal) : MapError(result);
    }

    private ActionResult MapError(CrmDealOperationResult result) => result.Error switch
    {
        CrmDealError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmDealError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
