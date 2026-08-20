using LimousineBooking.Application.Payments;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// The one abstraction the Application layer depends on for talking to a
/// payment provider — StripePaymentService (Infrastructure) is the only
/// production implementation today; FakePaymentService (Infrastructure,
/// selected when PaymentSettings.Enabled is false) lets the whole payment
/// flow run in tests/local dev without a real Stripe account. Nothing outside
/// Infrastructure ever references the Stripe SDK directly.
/// </summary>
public interface IPaymentService
{
    Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies the webhook signature and parses the payload. Throws <see cref="InvalidPaymentWebhookSignatureException"/> on a bad/missing signature — never returns an unverified event.</summary>
    Task<PaymentProviderWebhookEvent> ParseWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);

    Task<PaymentProviderRefund> RefundAsync(string providerPaymentId, decimal amount, string currency, CancellationToken cancellationToken = default);
}
