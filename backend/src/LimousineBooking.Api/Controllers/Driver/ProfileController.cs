using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Driver;

/// <summary>The authenticated driver's own profile (name, contact, vehicle) — read-only; reuses the same IDriverService the admin driver-management screens use.</summary>
[ApiController]
[Route("api/driver/profile")]
[Authorize(Roles = "Driver")]
public class ProfileController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IDriverService driverService, ICurrentUserService currentUser)
    {
        _driverService = driverService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<DriverResponse>> Get(CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        var driver = await _driverService.GetByIdAsync(driverId, cancellationToken);
        return driver is null ? NotFound(new { message = "Driver not found." }) : Ok(driver);
    }
}
