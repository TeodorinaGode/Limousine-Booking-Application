using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Payments;

/// <summary>
/// A dev-only stand-in for the Stripe-hosted checkout page, reachable only
/// when FakePaymentService is the active IPaymentService (PaymentSettings.Enabled
/// = false — see FakePaymentService's summary). Every action 404s otherwise, so
/// this is inert in any environment where real Stripe is configured. Clicking a
/// button here calls the exact same IPaymentWebhookService.HandleWebhookAsync
/// path a real Stripe webhook delivery would — only the transport is faked.
/// </summary>
[ApiController]
[Route("api/payments/fake-checkout")]
[AllowAnonymous]
public class FakeCheckoutController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentWebhookService _webhookService;

    public FakeCheckoutController(IPaymentService paymentService, IPaymentWebhookService webhookService)
    {
        _paymentService = paymentService;
        _webhookService = webhookService;
    }

    [HttpGet("{sessionId}")]
    public IActionResult Show(string sessionId, [FromQuery] string successUrl, [FromQuery] string cancelUrl)
    {
        if (_paymentService is not FakePaymentService)
            return NotFound();

        var encodedSuccessUrl = System.Net.WebUtility.HtmlEncode(successUrl);
        var encodedCancelUrl = System.Net.WebUtility.HtmlEncode(cancelUrl);

        var html = $$"""
            <!doctype html>
            <html><head><meta charset="utf-8"><title>Fake Checkout (dev only)</title>
            <style>
              body { font-family: system-ui, sans-serif; background:#111; color:#f5f5f5; display:flex; align-items:center; justify-content:center; min-height:100vh; margin:0; }
              .card { border:1px solid #333; border-radius:8px; padding:2rem; max-width:420px; text-align:center; }
              button { display:block; width:100%; margin:.5rem 0; padding:.75rem; border-radius:4px; border:1px solid #555; background:#222; color:#fff; cursor:pointer; font-size:1rem; }
              button:hover { background:#333; }
              p { color:#aaa; font-size:.85rem; }
            </style></head>
            <body>
              <div class="card">
                <h2>Fake Stripe Checkout</h2>
                <p>Development-only payment simulator. Session: {{sessionId}}</p>
                <form method="post" action="/api/payments/fake-checkout/{{sessionId}}/complete">
                  <input type="hidden" name="successUrl" value="{{encodedSuccessUrl}}" />
                  <input type="hidden" name="cancelUrl" value="{{encodedCancelUrl}}" />
                  <button type="submit" name="outcome" value="completed">Simulate Successful Payment</button>
                  <button type="submit" name="outcome" value="failed">Simulate Failed Payment</button>
                  <button type="submit" name="outcome" value="expired">Simulate Session Expired</button>
                </form>
              </div>
            </body></html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("{sessionId}/complete")]
    public async Task<IActionResult> Complete(string sessionId, [FromForm] string outcome, [FromForm] string successUrl, [FromForm] string cancelUrl, CancellationToken cancellationToken)
    {
        if (_paymentService is not FakePaymentService)
            return NotFound();

        var payload = JsonSerializer.Serialize(new FakePaymentService.FakeWebhookPayload
        {
            EventId = $"fake_evt_{Guid.NewGuid():N}",
            EventType = outcome,
            CheckoutSessionId = sessionId,
            ProviderPaymentId = outcome == "completed" ? $"fake_pi_{sessionId}" : null
        });

        await _webhookService.HandleWebhookAsync(payload, signatureHeader: string.Empty, cancellationToken);

        return Redirect(outcome == "completed" ? successUrl : cancelUrl);
    }
}
