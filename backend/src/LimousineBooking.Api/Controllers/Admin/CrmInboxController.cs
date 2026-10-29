using LimousineBooking.Application.Common;
using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only CRM inbox — searching synced email threads, viewing a
/// conversation, and editing its company/contact/deal associations. Sending
/// (Phase 5) and the connection/sync endpoints live in their own controllers.
/// </summary>
[ApiController]
[Route("api/admin/crm/email")]
[Authorize(Roles = "Administrator")]
public class CrmInboxController : ControllerBase
{
    private readonly ICrmInboxService _inboxService;

    public CrmInboxController(ICrmInboxService inboxService)
    {
        _inboxService = inboxService;
    }

    [HttpGet("threads")]
    public async Task<ActionResult<PagedResult<CrmEmailThreadListItemResponse>>> SearchThreads([FromQuery] CrmEmailThreadSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _inboxService.SearchThreadsAsync(query, cancellationToken));

    [HttpGet("threads/{id:guid}")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> GetThread(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _inboxService.GetThreadDetailAsync(id, cancellationToken);
        return detail is null ? NotFound(new { message = "Conversation not found." }) : Ok(detail);
    }

    [HttpDelete("threads/{id:guid}")]
    public async Task<IActionResult> RemoveThread(Guid id, CancellationToken cancellationToken)
    {
        var removed = await _inboxService.RemoveThreadAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound(new { message = "Conversation not found." });
    }

    [HttpPut("threads/{id:guid}/read")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inboxService.SetReadAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpPut("threads/{id:guid}/unread")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> MarkUnread(Guid id, CancellationToken cancellationToken)
    {
        var result = await _inboxService.SetReadAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpPost("threads/{id:guid}/companies")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> LinkCompany(Guid id, [FromBody] LinkCrmEmailThreadCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await _inboxService.LinkCompanyAsync(id, request.CompanyId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpDelete("threads/{id:guid}/companies/{companyId:guid}")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> UnlinkCompany(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        var result = await _inboxService.UnlinkCompanyAsync(id, companyId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpPost("threads/{id:guid}/contacts")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> LinkContact(Guid id, [FromBody] LinkCrmEmailThreadContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _inboxService.LinkContactAsync(id, request.ContactId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpDelete("threads/{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> UnlinkContact(Guid id, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _inboxService.UnlinkContactAsync(id, contactId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpPost("threads/{id:guid}/deals")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> LinkDeal(Guid id, [FromBody] LinkCrmEmailThreadDealRequest request, CancellationToken cancellationToken)
    {
        var result = await _inboxService.LinkDealAsync(id, request.DealId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpDelete("threads/{id:guid}/deals/{dealId:guid}")]
    public async Task<ActionResult<CrmEmailThreadDetailResponse>> UnlinkDeal(Guid id, Guid dealId, CancellationToken cancellationToken)
    {
        var result = await _inboxService.UnlinkDealAsync(id, dealId, cancellationToken);
        return result.Succeeded ? Ok(result.Thread) : MapError(result);
    }

    [HttpGet("messages/{messageId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var download = await _inboxService.GetAttachmentAsync(messageId, attachmentId, cancellationToken);
        return download is null
            ? NotFound(new { message = "Attachment not found." })
            : File(download.Content, download.ContentType, download.FileName);
    }

    private ActionResult MapError(CrmInboxOperationResult result) => result.Error switch
    {
        CrmInboxError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmInboxError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        CrmInboxError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
