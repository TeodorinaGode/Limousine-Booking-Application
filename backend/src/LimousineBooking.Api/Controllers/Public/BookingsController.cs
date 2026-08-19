using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Anonymous customer booking submission — no account or login required.</summary>
[ApiController]
[Route("api/public/bookings")]
[AllowAnonymous]
public class BookingsController : ControllerBase
{
    private readonly IPublicBookingService _publicBookingService;

    public BookingsController(IPublicBookingService publicBookingService)
    {
        _publicBookingService = publicBookingService;
    }

    /// <summary>
    /// Creates a new booking request. The booking is created with status "Pending" and no
    /// driver/vehicle assignment — those are handled separately by staff, not at submission time.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<BookingResponse>> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _publicBookingService.CreateBookingAsync(request, cancellationToken);

        // No public "get booking by reference" endpoint exists (customers have no
        // account to look bookings up through), so there's no GET action to point
        // a Location header at — just return the created resource in the body.
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Booking)
            : MapError(result);
    }

    private ActionResult MapError(BookingOperationResult result) => result.Error switch
    {
        BookingError.NotFound => NotFound(new { message = result.ErrorMessage }),
        BookingError.Conflict => Conflict(new { message = result.ErrorMessage }),
        BookingError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
