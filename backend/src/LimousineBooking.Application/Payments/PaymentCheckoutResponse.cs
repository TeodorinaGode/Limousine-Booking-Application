namespace LimousineBooking.Application.Payments;

/// <summary>Response for POST /api/public/bookings/{bookingReference}/payment and .../payment/retry — never includes any Stripe secret.</summary>
public class PaymentCheckoutResponse
{
    public Guid PaymentId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
