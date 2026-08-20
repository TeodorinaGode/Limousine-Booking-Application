using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Anonymous, read-only fleet listing for the public website's Fleet page. No authentication required.</summary>
[ApiController]
[Route("api/public/vehicles")]
[AllowAnonymous]
public class VehiclesController : ControllerBase
{
    private readonly IPublicVehicleService _publicVehicleService;

    public VehiclesController(IPublicVehicleService publicVehicleService)
    {
        _publicVehicleService = publicVehicleService;
    }

    /// <summary>Lists all active vehicles available for booking.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicVehicleResponse>>> GetActiveVehicles(CancellationToken cancellationToken)
    {
        return Ok(await _publicVehicleService.GetActiveVehiclesAsync(cancellationToken));
    }
}
