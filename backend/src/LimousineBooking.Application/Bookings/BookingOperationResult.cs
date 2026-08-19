namespace LimousineBooking.Application.Bookings;

public enum BookingError
{
    Validation,
    NotFound,
    Conflict
}

public class BookingOperationResult
{
    public bool Succeeded { get; }
    public BookingResponse? Booking { get; }
    public BookingError? Error { get; }
    public string? ErrorMessage { get; }

    private BookingOperationResult(bool succeeded, BookingResponse? booking, BookingError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Booking = booking;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static BookingOperationResult Success(BookingResponse booking) => new(true, booking, null, null);

    public static BookingOperationResult Failure(BookingError error, string message) => new(false, null, error, message);
}
