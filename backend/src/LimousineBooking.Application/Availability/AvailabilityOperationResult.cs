namespace LimousineBooking.Application.Availability;

public enum AvailabilityError
{
    Validation,
    NotFound,
    /// <summary>Overlaps another availability period for the same driver and date.</summary>
    Conflict
}

public class AvailabilityOperationResult
{
    public bool Succeeded { get; }
    public AvailabilityResponse? Availability { get; }
    public AvailabilityError? Error { get; }
    public string? ErrorMessage { get; }

    private AvailabilityOperationResult(bool succeeded, AvailabilityResponse? availability, AvailabilityError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Availability = availability;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static AvailabilityOperationResult Success(AvailabilityResponse availability) => new(true, availability, null, null);

    public static AvailabilityOperationResult Failure(AvailabilityError error, string message) => new(false, null, error, message);
}
