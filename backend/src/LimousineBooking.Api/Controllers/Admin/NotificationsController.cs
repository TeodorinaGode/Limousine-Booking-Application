using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only visibility into failed notification deliveries, and the
/// ability to retry them. Never exposes SMTP credentials or raw rendered email
/// bodies — only the metadata an administrator needs to triage a failure.
/// </summary>
[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Administrator")]
public class NotificationsController : ControllerBase
{
    private readonly IAdminNotificationService _adminNotificationService;

    public NotificationsController(IAdminNotificationService adminNotificationService)
    {
        _adminNotificationService = adminNotificationService;
    }

    /// <summary>Notifications that have exhausted all retries and been marked Failed.</summary>
    [HttpGet("failed")]
    public async Task<ActionResult<PagedResult<FailedNotificationResponse>>> GetFailed(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await _adminNotificationService.GetFailedAsync(page, pageSize, cancellationToken));
    }

    /// <summary>
    /// Resets a notification's retry state and puts it back into Pending — the
    /// background worker picks it up on its next poll. This endpoint never sends
    /// the email itself.
    /// </summary>
    /// <response code="404">No notification exists with the given id.</response>
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var found = await _adminNotificationService.RetryAsync(id, cancellationToken);
        return found ? NoContent() : NotFound(new { message = "Notification not found." });
    }
}
