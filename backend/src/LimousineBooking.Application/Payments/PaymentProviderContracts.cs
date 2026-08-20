namespace LimousineBooking.Application.Payments;

// The provider-agnostic contracts IPaymentService speaks in — Stripe-specific
// types (Stripe.Checkout.Session, Stripe.Event, ...) never leak past
// StripePaymentService/FakePaymentService, so the Application layer (and every
// caller of IPaymentService) has no compile-time dependency on the Stripe SDK.

public class PaymentCheckoutRequest
{
    public Guid PaymentId { get; init; }
    public Guid BookingId { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; }
}

public class PaymentCheckoutSession
{
    public string ProviderCheckoutSessionId { get; init; } = string.Empty;
    public string CheckoutUrl { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}

public enum PaymentProviderEventType
{
    Unknown,
    CheckoutCompleted,
    PaymentFailed,
    CheckoutExpired
}

/// <summary>The result of verifying + parsing one provider webhook delivery — never trusted until the signature has been checked (see IPaymentService.ParseWebhookEventAsync).</summary>
public class PaymentProviderWebhookEvent
{
    public string ProviderEventId { get; init; } = string.Empty;
    public PaymentProviderEventType EventType { get; init; }
    public string? CheckoutSessionId { get; init; }
    public string? ProviderPaymentId { get; init; }
    public string? FailureReason { get; init; }
}

public class PaymentProviderRefund
{
    public string ProviderRefundId { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
}

/// <summary>Thrown by ParseWebhookEventAsync when the webhook signature does not verify — the caller must reject the request, never process the payload.</summary>
public class InvalidPaymentWebhookSignatureException : Exception
{
    public InvalidPaymentWebhookSignatureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
