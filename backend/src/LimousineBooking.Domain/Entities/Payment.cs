using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// One payment attempt for a booking. A booking may have several Payment rows
/// over time (a failed/expired attempt followed by a successful retry) — see
/// class summary on the booking side; at most one attempt per booking should
/// ever reach <see cref="PaymentStatus.Paid"/>, enforced by the application
/// service (only starting a new attempt when no prior one succeeded), not by a
/// database constraint, since failed/cancelled attempts must remain for audit.
/// </summary>
public class Payment : AuditableEntity
{
    public Guid BookingId { get; private set; }
    public PaymentProvider Provider { get; private set; }

    /// <summary>Stripe PaymentIntent id — set only once the payment actually succeeds.</summary>
    public string? ProviderPaymentId { get; private set; }

    /// <summary>Stripe Checkout Session id — set at creation, before the customer ever reaches Stripe.</summary>
    public string? ProviderCheckoutSessionId { get; private set; }

    /// <summary>When the current checkout session stops being usable — lets a repeat "start payment" request reuse a still-valid session instead of creating a duplicate (sections 29/31) rather than requiring a live provider round trip to find out.</summary>
    public DateTime? CheckoutExpiresAt { get; private set; }

    /// <summary>The hosted checkout page URL, cached alongside the session id so reusing an open session never needs an extra provider round trip.</summary>
    public string? CheckoutUrl { get; private set; }

    /// <summary>The authoritative amount, copied from Booking.Price at the moment this attempt was created — never re-read from Route.Price.</summary>
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; private set; }
    public string? FailureReason { get; private set; }

    public Booking? Booking { get; private set; }

    private Payment()
    {
    }

    public Payment(Guid bookingId, PaymentProvider provider, decimal amount, string currency)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        BookingId = bookingId;
        Provider = provider;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    /// <summary>Recorded once the provider's checkout session has actually been created — the payment stays Pending.</summary>
    public void AttachCheckoutSession(string providerCheckoutSessionId, string checkoutUrl, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(providerCheckoutSessionId))
            throw new ArgumentException("ProviderCheckoutSessionId is required.", nameof(providerCheckoutSessionId));
        if (string.IsNullOrWhiteSpace(checkoutUrl))
            throw new ArgumentException("CheckoutUrl is required.", nameof(checkoutUrl));

        ProviderCheckoutSessionId = providerCheckoutSessionId;
        CheckoutUrl = checkoutUrl;
        CheckoutExpiresAt = expiresAtUtc;
    }

    /// <summary>For payment methods that confirm asynchronously (not used by standard card Checkout, but modeled for completeness).</summary>
    public void MarkProcessing()
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new InvalidOperationException($"A {Status} payment cannot move to Processing.");

        Status = PaymentStatus.Processing;
    }

    public void MarkPaid(string providerPaymentId, DateTime paidAtUtc)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            throw new ArgumentException("ProviderPaymentId is required.", nameof(providerPaymentId));
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new InvalidOperationException($"A {Status} payment cannot be marked Paid again.");

        ProviderPaymentId = providerPaymentId;
        Status = PaymentStatus.Paid;
        PaidAt = paidAtUtc;
        FailureReason = null;
    }

    public void MarkFailed(string? failureReason)
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new InvalidOperationException($"A {Status} payment cannot be marked Failed.");

        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
    }

    /// <summary>The Stripe Checkout Session expired (or was otherwise abandoned) without completing.</summary>
    public void MarkCancelled()
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new InvalidOperationException($"A {Status} payment cannot be cancelled.");

        Status = PaymentStatus.Cancelled;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Paid)
            throw new InvalidOperationException("Only a Paid payment can be refunded.");

        Status = PaymentStatus.Refunded;
    }
}
