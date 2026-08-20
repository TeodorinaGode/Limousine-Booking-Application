namespace LimousineBooking.Application.Interfaces;

public enum PaymentWebhookOutcome
{
    /// <summary>Signature verification failed — the caller must respond 400, never process the payload.</summary>
    InvalidSignature,

    /// <summary>Applied (or the event referred to a payment/booking not found in this system) — the caller always responds 200 so the provider stops retrying.</summary>
    Processed,

    /// <summary>This exact provider event id was already recorded — no business effect was (re)applied.</summary>
    AlreadyProcessed
}

/// <summary>
/// Applies the business consequences of a verified payment provider webhook
/// event: marks the Payment Paid/Failed/Cancelled, updates nothing on
/// BookingStatus (payment and booking confirmation are independent — see
/// Payment's class summary), and triggers the payment-succeeded notification
/// exactly once per payment. The webhook is the only source of truth for
/// "did the payment actually succeed" (section 20) — nothing else in this
/// application ever marks a Payment Paid.
/// </summary>
public interface IPaymentWebhookService
{
    Task<PaymentWebhookOutcome> HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);
}
