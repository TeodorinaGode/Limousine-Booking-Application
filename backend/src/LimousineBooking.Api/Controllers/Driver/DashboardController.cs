using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Driver;

/// <summary>The driver's landing view — today's trips (Europe/Zurich "today") plus forward-looking counts. Driver id always comes from the JWT, never the request.</summary>
[ApiController]
[Route("api/driver/dashboard")]
[Authorize(Roles = "Driver")]
public class DashboardController : ControllerBase
{
    private readonly IDriverBookingService _driverBookingService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IDriverBookingService driverBookingService, ICurrentUserService currentUser)
    {
        _driverBookingService = driverBookingService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<DriverDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        return Ok(await _driverBookingService.GetDashboardAsync(driverId, cancellationToken));
    }
}
