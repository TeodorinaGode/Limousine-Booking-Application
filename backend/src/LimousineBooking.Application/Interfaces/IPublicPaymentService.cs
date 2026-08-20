using LimousineBooking.Application.Payments;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// The anonymous customer's payment operations — every method requires both
/// bookingReference and the booking's PublicAccessToken (see Booking.PublicAccessToken)
/// so payment status can never be read by guessing a reference.
/// </summary>
public interface IPublicPaymentService
{
    Task<PaymentOperationResult> CreatePaymentAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> RetryPaymentAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Null if the booking/token pair doesn't match, or no payment attempt exists yet.</summary>
    Task<PublicPaymentStatusResponse?> GetPaymentStatusAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default);
}
