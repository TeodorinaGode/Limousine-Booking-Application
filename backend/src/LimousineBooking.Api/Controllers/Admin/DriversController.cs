using LimousineBooking.Application.Common;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only driver management: creates the linked User (Role=Driver)
/// and Driver profile together, edits, activation/deactivation (which also
/// flips the linked User's login access), vehicle assignment, and password
/// resets. Deactivating a driver never deletes the Driver or User records —
/// historical booking references must remain valid.
/// </summary>
[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = "Administrator")]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    /// <summary>List/search drivers with optional filtering, sorting, and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DriverResponse>>> GetAll([FromQuery] DriverSearchQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _driverService.SearchAsync(query, cancellationToken));
    }

    /// <summary>Get a single driver by id, including user info and current vehicle.</summary>
    /// <response code="404">No driver exists with the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DriverResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var driver = await _driverService.GetByIdAsync(id, cancellationToken);
        return driver is null ? NotFound(new { message = "Driver not found." }) : Ok(driver);
    }

    /// <summary>
    /// Creates a User (Role=Driver, password hashed) and its linked Driver
    /// profile together. An optional vehicle must be active and not already
    /// assigned to another driver.
    /// </summary>
    /// <response code="409">Email already in use, or the vehicle is already assigned to another driver.</response>
    [HttpPost]
    public async Task<ActionResult<DriverResponse>> Create([FromBody] CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _driverService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Driver!.Id }, result.Driver)
            : MapError(result);
    }

    /// <summary>Full update (name, email, phone, active status, vehicle). Role can never be changed here.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DriverResponse>> Update(Guid id, [FromBody] UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Driver) : MapError(result);
    }

    /// <summary>Convenience endpoint to activate a driver (and their login) without resending the full form.</summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<DriverResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _driverService.SetActiveAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Driver) : MapError(result);
    }

    /// <summary>Convenience endpoint to deactivate a driver (and their login). Never deletes the driver.</summary>
    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<DriverResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _driverService.SetActiveAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Driver) : MapError(result);
    }

    /// <summary>Resets a driver's password. The new password is hashed and never returned.</summary>
    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<DriverResponse>> ResetPassword(Guid id, [FromBody] ResetDriverPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _driverService.ResetPasswordAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Driver) : MapError(result);
    }

    private ActionResult MapError(DriverOperationResult result) => result.Error switch
    {
        DriverError.NotFound => NotFound(new { message = result.ErrorMessage }),
        DriverError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        DriverError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
