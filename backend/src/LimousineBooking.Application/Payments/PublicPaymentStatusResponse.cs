namespace LimousineBooking.Application.Payments;

/// <summary>
/// Response for GET /api/public/bookings/{bookingReference}/payment — deliberately
/// exposes only public-safe fields (section 40). Never the internal Payment id,
/// Stripe identifiers, webhook data, or anything else an anonymous customer
/// shouldn't see.
/// </summary>
public class PublicPaymentStatusResponse
{
    public string BookingReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
