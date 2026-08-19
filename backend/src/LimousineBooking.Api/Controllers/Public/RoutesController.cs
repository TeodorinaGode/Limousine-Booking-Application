using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Anonymous, read-only route listing for the public booking flow. No authentication required.</summary>
[ApiController]
[Route("api/public/routes")]
[AllowAnonymous]
public class RoutesController : ControllerBase
{
    private readonly IPublicBookingService _publicBookingService;

    public RoutesController(IPublicBookingService publicBookingService)
    {
        _publicBookingService = publicBookingService;
    }

    /// <summary>Lists all active routes available for booking.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicRouteResponse>>> GetActiveRoutes(CancellationToken cancellationToken)
    {
        return Ok(await _publicBookingService.GetActiveRoutesAsync(cancellationToken));
    }
}
