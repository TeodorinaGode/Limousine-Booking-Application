using LimousineBooking.Application.Availability;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Driver;

/// <summary>
/// Driver self-service: current-availability toggle and their own schedule
/// CRUD. The driver id always comes from the authenticated JWT's "driverId"
/// claim (ICurrentUserService), never from the request — a driver can only
/// ever see or change their own data.
/// </summary>
[ApiController]
[Route("api/driver/availability")]
[Authorize(Roles = "Driver")]
public class AvailabilityController : ControllerBase
{
    private readonly IDriverAvailabilityService _availabilityService;
    private readonly ICurrentUserService _currentUser;

    public AvailabilityController(IDriverAvailabilityService availabilityService, ICurrentUserService currentUser)
    {
        _availabilityService = availabilityService;
        _currentUser = currentUser;
    }

    /// <summary>Current availability status plus the authenticated driver's own schedule, optionally date-filtered.</summary>
    [HttpGet]
    public async Task<ActionResult<DriverScheduleResponse>> GetSchedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (!TryGetDriverId(out var driverId, out var problem))
            return problem;

        var schedule = await _availabilityService.GetScheduleAsync(driverId, from, to, cancellationToken);
        return schedule is null ? NotFound(new { message = "Driver not found." }) : Ok(schedule);
    }

    /// <summary>Sets the driver's real-time availability flag (separate from the schedule).</summary>
    [HttpPut]
    public async Task<IActionResult> SetCurrentAvailability([FromBody] SetCurrentAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetDriverId(out var driverId, out var problem))
            return problem;

        var result = await _availabilityService.SetCurrentAvailabilityAsync(driverId, request.IsAvailable, cancellationToken);
        return result is null ? NotFound(new { message = "Driver not found." }) : Ok(new { isAvailable = result.Value });
    }

    /// <summary>Creates a new availability period. Rejected for inactive drivers, or if it overlaps an existing period.</summary>
    /// <response code="409">Overlaps an existing availability period for the same date.</response>
    [HttpPost]
    public async Task<ActionResult<AvailabilityResponse>> Create([FromBody] CreateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetDriverId(out var driverId, out var problem))
            return problem;

        var result = await _availabilityService.CreateAsync(driverId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Availability) : MapError(result);
    }

    /// <summary>Updates one of the authenticated driver's own availability periods.</summary>
    /// <response code="404">The record doesn't exist, or belongs to a different driver.</response>
    /// <response code="409">Overlaps an existing availability period for the same date.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AvailabilityResponse>> Update(Guid id, [FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetDriverId(out var driverId, out var problem))
            return problem;

        var result = await _availabilityService.UpdateAsync(driverId, id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Availability) : MapError(result);
    }

    /// <summary>Deletes one of the authenticated driver's own availability periods.</summary>
    /// <response code="404">The record doesn't exist, or belongs to a different driver.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetDriverId(out var driverId, out var problem))
            return problem;

        var result = await _availabilityService.DeleteAsync(driverId, id, cancellationToken);
        return result.Succeeded ? NoContent() : MapError(result);
    }

    private bool TryGetDriverId(out Guid driverId, out ActionResult problem)
    {
        if (_currentUser.DriverId.HasValue)
        {
            driverId = _currentUser.DriverId.Value;
            problem = null!;
            return true;
        }

        driverId = Guid.Empty;
        // A Driver-role token should always carry a driverId claim (see
        // JwtTokenService/LoginHandler) — reaching here means an inconsistent
        // token, not a normal user-facing error.
        problem = StatusCode(500, new { message = "Driver identity missing from token." });
        return false;
    }

    private ActionResult MapError(AvailabilityOperationResult result) => result.Error switch
    {
        AvailabilityError.NotFound => NotFound(new { message = result.ErrorMessage }),
        AvailabilityError.Conflict => Conflict(new { message = result.ErrorMessage }),
        AvailabilityError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
