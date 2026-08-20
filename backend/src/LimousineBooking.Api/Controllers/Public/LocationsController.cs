using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Map;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Public service-area map pins + rendering settings (Prompt 19). No authentication, no account required.</summary>
[ApiController]
[Route("api/public/locations")]
[AllowAnonymous]
public class LocationsController : ControllerBase
{
    private readonly IPublicLocationService _publicLocationService;

    public LocationsController(IPublicLocationService publicLocationService)
    {
        _publicLocationService = publicLocationService;
    }

    [HttpGet]
    public async Task<ActionResult<PublicLocationsResponse>> GetLocations(CancellationToken cancellationToken)
    {
        return Ok(await _publicLocationService.GetLocationsAsync(cancellationToken));
    }
}
