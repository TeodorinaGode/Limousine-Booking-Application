namespace LimousineBooking.Application.Payments;

public enum PaymentError
{
    Validation,
    NotFound,
    Conflict,
    ProviderError
}

/// <summary>Stable, documented error codes (section 60) — the frontend matches on <see cref="PaymentOperationResult.ErrorCode"/>, never on ErrorMessage's exact wording.</summary>
public static class PaymentErrorCodes
{
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string BookingNotPayable = "BOOKING_NOT_PAYABLE";
    public const string BookingAlreadyPaid = "BOOKING_ALREADY_PAID";
    public const string BookingCancelled = "BOOKING_CANCELLED";
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";
    public const string PaymentAlreadyCompleted = "PAYMENT_ALREADY_COMPLETED";
    public const string PaymentSessionExpired = "PAYMENT_SESSION_EXPIRED";
    public const string PaymentProviderError = "PAYMENT_PROVIDER_ERROR";
    public const string PaymentConfigurationError = "PAYMENT_CONFIGURATION_ERROR";
    public const string InvalidPaymentWebhook = "INVALID_PAYMENT_WEBHOOK";
}

public class PaymentOperationResult
{
    public bool Succeeded { get; }
    public PaymentCheckoutResponse? Checkout { get; }
    public PaymentError? Error { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private PaymentOperationResult(bool succeeded, PaymentCheckoutResponse? checkout, PaymentError? error, string? errorCode, string? errorMessage)
    {
        Succeeded = succeeded;
        Checkout = checkout;
        Error = error;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static PaymentOperationResult Success(PaymentCheckoutResponse checkout) => new(true, checkout, null, null, null);

    public static PaymentOperationResult Failure(PaymentError error, string errorCode, string message) => new(false, null, error, errorCode, message);
}
