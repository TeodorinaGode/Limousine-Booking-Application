using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only booking management: search/detail, editing, manual driver
/// assignment/reassignment, cancellation, and triggering automatic reassignment.
/// All assignment decisions ultimately go through AutomaticAssignmentService
/// (Prompt 9) or its own eligibility checks — this controller stays thin.
/// </summary>
[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Administrator")]
public class BookingsController : ControllerBase
{
    private readonly IAdminBookingService _adminBookingService;

    public BookingsController(IAdminBookingService adminBookingService)
    {
        _adminBookingService = adminBookingService;
    }

    /// <summary>List/search bookings with filtering, sorting, and pagination. Returns only the fields the admin table displays.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminBookingListItemResponse>>> GetAll([FromQuery] AdminBookingSearchQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _adminBookingService.SearchAsync(query, cancellationToken));
    }

    /// <summary>Operational counters + the next 10 upcoming trips for the admin dashboard.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _adminBookingService.GetDashboardAsync(cancellationToken));
    }

    /// <summary>Full booking detail, including assignment internals and history — never exposed to the public API.</summary>
    /// <response code="404">No booking exists with the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminBookingDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _adminBookingService.GetByIdAsync(id, cancellationToken);
        return booking is null ? NotFound(new { message = "Booking not found." }) : Ok(booking);
    }

    /// <summary>
    /// Edits trip/customer details. Changing the route, date, time, or passenger count
    /// revalidates the current assignment (via AutomaticAssignmentService) — the
    /// booking may end up Confirmed with the same or a different driver, or Pending
    /// with RequiresManualAssignment. Status/price/driver/vehicle cannot be set directly here.
    /// </summary>
    /// <response code="404">No booking or route exists with the given id.</response>
    /// <response code="409">The booking is Cancelled/Completed, or the route is inactive.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminBookingDetailResponse>> Update(Guid id, [FromBody] UpdateBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminBookingService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    /// <summary>
    /// Manually assigns (or reassigns) a driver + vehicle. Fully revalidated
    /// server-side — driver/vehicle status, availability, schedule, conflicts, and
    /// driver/vehicle compatibility. Runs inside the same Serializable-transaction
    /// protection as automatic assignment (Prompt 9) to prevent double-booking.
    /// </summary>
    /// <response code="404">No booking/driver/vehicle exists with the given id.</response>
    /// <response code="409">The chosen driver/vehicle is not a valid assignment for this booking.</response>
    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<AdminBookingDetailResponse>> Assign(Guid id, [FromBody] AssignDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminBookingService.AssignDriverAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    /// <summary>Re-runs automatic assignment (Prompt 9) for this booking — e.g. after a driver becomes available again.</summary>
    /// <response code="409">The booking is Cancelled/Completed.</response>
    [HttpPost("{id:guid}/auto-assign")]
    public async Task<ActionResult<AdminBookingDetailResponse>> AutoAssign(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminBookingService.AutoAssignAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    /// <summary>
    /// Cancels a booking. The record, its reference, and its historical price are
    /// kept — only the status changes and the driver/vehicle are released.
    /// </summary>
    /// <response code="409">The booking is already Cancelled, or is Completed.</response>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<AdminBookingDetailResponse>> Cancel(Guid id, [FromBody] CancelBookingRequest? request, CancellationToken cancellationToken)
    {
        var result = await _adminBookingService.CancelAsync(id, request ?? new CancelBookingRequest(), cancellationToken);
        return result.Succeeded ? Ok(result.Booking) : MapError(result);
    }

    private ActionResult MapError(AdminBookingOperationResult result) => result.Error switch
    {
        AdminBookingError.NotFound => NotFound(new { message = result.ErrorMessage }),
        AdminBookingError.Conflict => Conflict(new { message = result.ErrorMessage }),
        AdminBookingError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
