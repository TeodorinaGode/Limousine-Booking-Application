using LimousineBooking.Application.Common;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Driver;

/// <summary>
/// The authenticated driver's own bookings: schedule, trip detail, and ride-status
/// transitions. Driver id always comes from the JWT's "driverId" claim, never the
/// request — a driver can only ever see or act on their own bookings; any other
/// booking id (including one belonging to a different driver) resolves to 404.
/// </summary>
[ApiController]
[Route("api/driver/bookings")]
[Authorize(Roles = "Driver")]
public class BookingsController : ControllerBase
{
    private readonly IDriverBookingService _driverBookingService;
    private readonly ICurrentUserService _currentUser;

    public BookingsController(IDriverBookingService driverBookingService, ICurrentUserService currentUser)
    {
        _driverBookingService = driverBookingService;
        _currentUser = currentUser;
    }

    /// <summary>The driver's own schedule, optionally date-filtered, chronological order.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DriverBookingListItemResponse>>> GetAll([FromQuery] DriverBookingSearchQuery query, CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        return Ok(await _driverBookingService.SearchAsync(driverId, query, cancellationToken));
    }

    /// <summary>Full trip detail, including ride-status history.</summary>
    /// <response code="404">No booking exists with the given id, or it belongs to a different driver.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DriverBookingDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        var booking = await _driverBookingService.GetByIdAsync(driverId, id, cancellationToken);
        return booking is null ? NotFound(new { message = "Booking not found." }) : Ok(booking);
    }

    /// <summary>Upcoming -&gt; OnTheWay.</summary>
    /// <response code="404">No booking exists with the given id, or it belongs to a different driver.</response>
    /// <response code="409">The ride is not in the Upcoming state (already started, completed, or the booking is cancelled), or the driver is not active.</response>
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<DriverBookingDetailResponse>> Start(Guid id, CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        var result = await _driverBookingService.StartRideAsync(driverId, id, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    /// <summary>OnTheWay -&gt; PassengerPickedUp.</summary>
    /// <response code="404">No booking exists with the given id, or it belongs to a different driver.</response>
    /// <response code="409">The ride is not in the OnTheWay state, or the driver is not active.</response>
    [HttpPost("{id:guid}/pickup")]
    public async Task<ActionResult<DriverBookingDetailResponse>> Pickup(Guid id, CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        var result = await _driverBookingService.MarkPassengerPickedUpAsync(driverId, id, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    /// <summary>PassengerPickedUp -&gt; Completed. Also moves the booking's Status to Completed and notifies the customer.</summary>
    /// <response code="404">No booking exists with the given id, or it belongs to a different driver.</response>
    /// <response code="409">The ride is not in the PassengerPickedUp state, or the driver is not active.</response>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<DriverBookingDetailResponse>> Complete(Guid id, CancellationToken cancellationToken)
    {
        if (!DriverIdentity.TryGetDriverId(_currentUser, out var driverId, out var problem))
            return problem!;

        var result = await _driverBookingService.CompleteRideAsync(driverId, id, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    private ActionResult MapError(DriverBookingOperationResult result) => result.Error switch
    {
        DriverBookingError.NotFound => NotFound(new { message = result.ErrorMessage }),
        DriverBookingError.Conflict => Conflict(new { message = result.ErrorMessage }),
        DriverBookingError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
