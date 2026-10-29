using LimousineBooking.Application.AdminAlerts;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>The notification bell's backing API — new bookings and unread CRM emails since this admin last checked.</summary>
[ApiController]
[Route("api/admin/alerts")]
[Authorize(Roles = "Administrator")]
public class AdminAlertsController : ControllerBase
{
    private readonly IAdminAlertsService _alertsService;
    private readonly ICurrentUserService _currentUserService;

    public AdminAlertsController(IAdminAlertsService alertsService, ICurrentUserService currentUserService)
    {
        _alertsService = alertsService;
        _currentUserService = currentUserService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminAlertsSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        return Ok(await _alertsService.GetSummaryAsync(userId, cancellationToken));
    }

    [HttpPost("bookings/seen")]
    public async Task<IActionResult> MarkBookingsSeen(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        await _alertsService.MarkBookingsSeenAsync(userId, cancellationToken);
        return NoContent();
    }
}
