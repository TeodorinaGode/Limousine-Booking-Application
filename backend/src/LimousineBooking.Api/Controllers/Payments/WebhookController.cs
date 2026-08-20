using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Payments;

/// <summary>
/// Receives Stripe (or the fake provider's simulated) webhook deliveries. The
/// raw request body is read byte-for-byte — Stripe's signature is computed over
/// the exact bytes sent, so any JSON re-serialization before verification would
/// invalidate it. This is the only place a payment is ever marked Paid
/// (section 20) — nothing in the request/redirect path does that.
/// </summary>
[ApiController]
[Route("api/payments/webhook")]
[AllowAnonymous]
public class WebhookController : ControllerBase
{
    private const string StripeSignatureHeader = "Stripe-Signature";

    private readonly IPaymentWebhookService _webhookService;

    public WebhookController(IPaymentWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[StripeSignatureHeader].ToString();

        var outcome = await _webhookService.HandleWebhookAsync(payload, signature, cancellationToken);

        return outcome switch
        {
            PaymentWebhookOutcome.InvalidSignature => BadRequest(new { code = PaymentErrorCodes.InvalidPaymentWebhook, message = "Invalid webhook signature." }),
            // Stripe (and the fake provider) retry on anything but 2xx — both a
            // freshly-applied event and an already-processed duplicate must
            // return success so the provider stops redelivering it.
            _ => Ok()
        };
    }
}
