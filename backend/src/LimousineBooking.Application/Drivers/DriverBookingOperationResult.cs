namespace LimousineBooking.Application.Drivers;

public enum DriverBookingError
{
    Validation,
    NotFound,
    Conflict
}

public class DriverBookingOperationResult
{
    public bool Succeeded { get; }
    public DriverBookingDetailResponse? Booking { get; }
    public DriverBookingError? Error { get; }
    public string? ErrorMessage { get; }

    private DriverBookingOperationResult(bool succeeded, DriverBookingDetailResponse? booking, DriverBookingError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Booking = booking;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static DriverBookingOperationResult Success(DriverBookingDetailResponse booking) => new(true, booking, null, null);

    public static DriverBookingOperationResult Failure(DriverBookingError error, string message) => new(false, null, error, message);
}
