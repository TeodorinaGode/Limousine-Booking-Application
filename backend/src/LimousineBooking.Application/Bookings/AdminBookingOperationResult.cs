namespace LimousineBooking.Application.Bookings;

public enum AdminBookingError
{
    Validation,
    NotFound,
    Conflict
}

public class AdminBookingOperationResult
{
    public bool Succeeded { get; }
    public AdminBookingDetailResponse? Booking { get; }
    public AdminBookingError? Error { get; }
    public string? ErrorMessage { get; }

    private AdminBookingOperationResult(bool succeeded, AdminBookingDetailResponse? booking, AdminBookingError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Booking = booking;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static AdminBookingOperationResult Success(AdminBookingDetailResponse booking) => new(true, booking, null, null);

    public static AdminBookingOperationResult Failure(AdminBookingError error, string message) => new(false, null, error, message);
}
