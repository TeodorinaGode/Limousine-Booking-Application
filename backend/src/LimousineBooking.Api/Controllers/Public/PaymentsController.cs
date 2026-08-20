using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>
/// Anonymous customer payment operations — every action requires both the
/// booking reference (in the route) and its PublicAccessToken (query string
/// "token"), since BookingReference alone is only a 6-digit random suffix and
/// not a secure access boundary (section 41).
/// </summary>
[ApiController]
[Route("api/public/bookings/{bookingReference}/payment")]
[AllowAnonymous]
public class PaymentsController : ControllerBase
{
    private readonly IPublicPaymentService _paymentService;

    public PaymentsController(IPublicPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Starts a new payment attempt (or hands back a still-open one — see PublicPaymentService).</summary>
    /// <response code="409">The booking is cancelled, already paid, or otherwise not payable.</response>
    [HttpPost]
    public async Task<ActionResult<PaymentCheckoutResponse>> Create(string bookingReference, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreatePaymentAsync(bookingReference, token, cancellationToken);
        return result.Succeeded ? Ok(result.Checkout) : MapError(result);
    }

    /// <summary>Starts a fresh payment attempt after a prior one failed/expired. The failed attempt is kept for audit (section 26).</summary>
    /// <response code="409">The booking is cancelled, already paid, or otherwise not payable.</response>
    [HttpPost("retry")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Retry(string bookingReference, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _paymentService.RetryPaymentAsync(bookingReference, token, cancellationToken);
        return result.Succeeded ? Ok(result.Checkout) : MapError(result);
    }

    /// <summary>
    /// The authoritative payment status — only the webhook ever marks a payment
    /// Paid, so this always reflects what the provider actually confirmed, never
    /// what the browser assumes after returning from checkout (section 18).
    /// </summary>
    /// <response code="404">No booking/token match, or no payment attempt exists yet.</response>
    [HttpGet]
    public async Task<ActionResult<PublicPaymentStatusResponse>> GetStatus(string bookingReference, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var status = await _paymentService.GetPaymentStatusAsync(bookingReference, token, cancellationToken);
        return status is null ? NotFound(new { code = PaymentErrorCodes.PaymentNotFound, message = "No payment found for this booking." }) : Ok(status);
    }

    private ActionResult MapError(PaymentOperationResult result) => result.Error switch
    {
        PaymentError.NotFound => NotFound(new { code = result.ErrorCode, message = result.ErrorMessage }),
        PaymentError.Conflict => Conflict(new { code = result.ErrorCode, message = result.ErrorMessage }),
        PaymentError.Validation => BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage }),
        PaymentError.ProviderError => StatusCode(StatusCodes.Status502BadGateway, new { code = result.ErrorCode, message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
