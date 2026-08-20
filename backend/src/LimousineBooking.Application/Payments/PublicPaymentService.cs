using System.Security.Cryptography;
using System.Text;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainPayment = LimousineBooking.Domain.Entities.Payment;

namespace LimousineBooking.Application.Payments;

/// <summary>
/// The anonymous customer's payment operations (create, retry, status) — the
/// counterpart to PublicBookingService for the payment step of the flow.
/// Never trusts an amount/currency from the caller: both always come from
/// Booking.Price/Currency, the historical snapshot taken at booking creation
/// (section 11/59), never the route's current price.
/// </summary>
public class PublicPaymentService : IPublicPaymentService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentService _paymentService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PublicPaymentService> _logger;

    public PublicPaymentService(
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IPaymentService paymentService,
        IDateTimeProvider dateTimeProvider,
        IOptions<PaymentSettings> settings,
        ILogger<PublicPaymentService> logger)
    {
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<PaymentOperationResult> CreatePaymentAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default) =>
        StartPaymentAsync(bookingReference, accessToken, allowReuseOfOpenSession: true, cancellationToken);

    /// <summary>Always opens a brand-new attempt (never reuses a Failed/Cancelled session) while keeping every prior attempt for audit (section 26).</summary>
    public Task<PaymentOperationResult> RetryPaymentAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default) =>
        StartPaymentAsync(bookingReference, accessToken, allowReuseOfOpenSession: false, cancellationToken);

    public async Task<PublicPaymentStatusResponse?> GetPaymentStatusAsync(string bookingReference, string accessToken, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByReferenceAsync(bookingReference, cancellationToken);
        if (booking is null || !TokensMatch(booking.PublicAccessToken, accessToken))
            return null;

        var payment = await _paymentRepository.GetLatestByBookingIdAsync(booking.Id, cancellationToken);
        if (payment is null)
            return null;

        return ToPublicStatus(bookingReference, payment);
    }

    private async Task<PaymentOperationResult> StartPaymentAsync(string bookingReference, string accessToken, bool allowReuseOfOpenSession, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByReferenceAsync(bookingReference, cancellationToken);
        if (booking is null || !TokensMatch(booking.PublicAccessToken, accessToken))
            return PaymentOperationResult.Failure(PaymentError.NotFound, PaymentErrorCodes.BookingNotFound, "Booking not found.");
        if (booking.Route is null)
            return PaymentOperationResult.Failure(PaymentError.Conflict, PaymentErrorCodes.BookingNotPayable, "The booking's route could not be loaded.");

        if (booking.Status == BookingStatus.Cancelled)
            return PaymentOperationResult.Failure(PaymentError.Conflict, PaymentErrorCodes.BookingCancelled, "This booking has been cancelled and cannot be paid.");
        if (booking.Status == BookingStatus.Completed)
            return PaymentOperationResult.Failure(PaymentError.Conflict, PaymentErrorCodes.BookingNotPayable, "This booking cannot be paid.");

        var existingPaid = await _paymentRepository.GetPaidByBookingIdAsync(booking.Id, cancellationToken);
        if (existingPaid is not null)
            return PaymentOperationResult.Failure(PaymentError.Conflict, PaymentErrorCodes.BookingAlreadyPaid, "This booking has already been paid.");

        var now = _dateTimeProvider.UtcNow;

        if (allowReuseOfOpenSession)
        {
            var latest = await _paymentRepository.GetLatestByBookingIdAsync(booking.Id, cancellationToken);
            if (latest is { Status: PaymentStatus.Pending, ProviderCheckoutSessionId: not null, CheckoutUrl: not null, CheckoutExpiresAt: not null } && latest.CheckoutExpiresAt > now)
            {
                // Prevents a double-click or a second browser tab from spawning a
                // second Stripe Checkout Session for the same attempt (sections 29-31) —
                // the customer is simply handed the same still-open session back.
                return PaymentOperationResult.Success(new PaymentCheckoutResponse
                {
                    PaymentId = latest.Id,
                    CheckoutUrl = latest.CheckoutUrl,
                    ExpiresAt = latest.CheckoutExpiresAt.Value
                });
            }
        }

        var payment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        await _paymentRepository.AddAsync(payment, cancellationToken);

        PaymentCheckoutSession session;
        try
        {
            session = await _paymentService.CreateCheckoutSessionAsync(new PaymentCheckoutRequest
            {
                PaymentId = payment.Id,
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                Amount = booking.Price,
                Currency = booking.Currency,
                CustomerEmail = booking.CustomerEmail,
                Description = $"{booking.Route.DepartureLocation} to {booking.Route.Destination} — {booking.BookingReference}",
                SuccessUrl = BuildRedirectUrl(_settings.SuccessUrl, booking.BookingReference, booking.PublicAccessToken, includeSessionPlaceholder: true),
                CancelUrl = BuildRedirectUrl(_settings.CancelUrl, booking.BookingReference, booking.PublicAccessToken, includeSessionPlaceholder: false),
                ExpiresInMinutes = _settings.CheckoutExpirationMinutes
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a payment provider checkout session for booking {BookingReference}.", booking.BookingReference);
            return PaymentOperationResult.Failure(PaymentError.ProviderError, PaymentErrorCodes.PaymentProviderError, "Unable to start payment right now. Please try again shortly.");
        }

        payment.AttachCheckoutSession(session.ProviderCheckoutSessionId, session.CheckoutUrl, session.ExpiresAtUtc);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment {PaymentId} checkout session created for booking {BookingReference}.", payment.Id, booking.BookingReference);

        return PaymentOperationResult.Success(new PaymentCheckoutResponse
        {
            PaymentId = payment.Id,
            CheckoutUrl = session.CheckoutUrl,
            ExpiresAt = session.ExpiresAtUtc
        });
    }

    /// <summary>Appends the booking reference + access token (and, for the success URL, Stripe's own {CHECKOUT_SESSION_ID} template placeholder) so the frontend redirect target always knows which booking it's looking at.</summary>
    private static string BuildRedirectUrl(string baseUrl, string bookingReference, string accessToken, bool includeSessionPlaceholder)
    {
        var separator = baseUrl.Contains('?') ? "&" : "?";
        var url = $"{baseUrl}{separator}ref={Uri.EscapeDataString(bookingReference)}&token={Uri.EscapeDataString(accessToken)}";
        return includeSessionPlaceholder ? $"{url}&session_id={{CHECKOUT_SESSION_ID}}" : url;
    }

    /// <summary>Constant-time comparison — an access token is a bearer secret, so even the comparison itself must not leak timing information.</summary>
    private static bool TokensMatch(string expected, string? actual)
    {
        if (string.IsNullOrEmpty(actual))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static PublicPaymentStatusResponse ToPublicStatus(string bookingReference, DomainPayment payment) => new()
    {
        BookingReference = bookingReference,
        Status = payment.Status.ToString(),
        Amount = payment.Amount,
        Currency = payment.Currency,
        PaidAt = payment.PaidAt
    };
}
