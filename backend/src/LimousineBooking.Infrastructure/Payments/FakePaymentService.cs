using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.Payments;

/// <summary>
/// Stands in for Stripe when PaymentSettings.Enabled is false — local
/// development without a Stripe account, and every automated test (section 51).
/// Never selected in production: Infrastructure.DependencyInjection only wires
/// this up when Enabled is explicitly false, and throws at startup instead of
/// silently falling back to it if Enabled is true but Stripe isn't configured
/// (section 52). CreateCheckoutSessionAsync points the customer at
/// FakeCheckoutController (Api), a dev-only page with "Simulate Success/Failure/
/// Expiry" buttons that call the exact same webhook code path as a real Stripe
/// delivery — nothing about payment *processing* is bypassed, only the provider.
/// </summary>
public class FakePaymentService : IPaymentService
{
    private readonly PaymentSettings _settings;

    public FakePaymentService(IOptions<PaymentSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = $"fake_cs_{Guid.NewGuid():N}";
        var expiresAt = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes);
        var baseUrl = _settings.FakeCheckoutBaseUrl.TrimEnd('/');

        var checkoutUrl =
            $"{baseUrl}/api/payments/fake-checkout/{sessionId}" +
            $"?successUrl={Uri.EscapeDataString(request.SuccessUrl)}" +
            $"&cancelUrl={Uri.EscapeDataString(request.CancelUrl)}";

        return Task.FromResult(new PaymentCheckoutSession
        {
            ProviderCheckoutSessionId = sessionId,
            CheckoutUrl = checkoutUrl,
            ExpiresAtUtc = expiresAt
        });
    }

    /// <summary>
    /// No real signature exists in Fake mode — the payload is the exact JSON
    /// shape FakeWebhookPayload below, produced only by FakeCheckoutController
    /// or a test, never by an external caller (this class is never reachable
    /// when Enabled is true, so nothing untrusted can ever call the real
    /// webhook endpoint expecting Fake-mode parsing).
    /// </summary>
    public Task<PaymentProviderWebhookEvent> ParseWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        FakeWebhookPayload? fake;
        try
        {
            fake = JsonSerializer.Deserialize<FakeWebhookPayload>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidPaymentWebhookSignatureException("Malformed fake webhook payload.", ex);
        }

        if (fake is null || string.IsNullOrWhiteSpace(fake.EventId) || string.IsNullOrWhiteSpace(fake.CheckoutSessionId))
            throw new InvalidPaymentWebhookSignatureException("Fake webhook payload is missing required fields.");

        var eventType = fake.EventType?.ToLowerInvariant() switch
        {
            "completed" => PaymentProviderEventType.CheckoutCompleted,
            "failed" => PaymentProviderEventType.PaymentFailed,
            "expired" => PaymentProviderEventType.CheckoutExpired,
            _ => PaymentProviderEventType.Unknown
        };

        return Task.FromResult(new PaymentProviderWebhookEvent
        {
            ProviderEventId = fake.EventId,
            EventType = eventType,
            CheckoutSessionId = fake.CheckoutSessionId,
            ProviderPaymentId = fake.ProviderPaymentId ?? $"fake_pi_{fake.CheckoutSessionId}",
            FailureReason = eventType == PaymentProviderEventType.PaymentFailed ? "Simulated failure." : null
        });
    }

    public Task<PaymentProviderRefund> RefundAsync(string providerPaymentId, decimal amount, string currency, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentProviderRefund { ProviderRefundId = $"fake_re_{Guid.NewGuid():N}", Succeeded = true });

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public class FakeWebhookPayload
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string CheckoutSessionId { get; set; } = string.Empty;
        public string? ProviderPaymentId { get; set; }
    }
}
