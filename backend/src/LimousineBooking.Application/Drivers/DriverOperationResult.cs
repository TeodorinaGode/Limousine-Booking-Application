namespace LimousineBooking.Application.Drivers;

public enum DriverError
{
    Validation,
    NotFound,
    /// <summary>Covers both a duplicate email and a vehicle already assigned to another driver.</summary>
    Duplicate
}

public class DriverOperationResult
{
    public bool Succeeded { get; }
    public DriverResponse? Driver { get; }
    public DriverError? Error { get; }
    public string? ErrorMessage { get; }

    private DriverOperationResult(bool succeeded, DriverResponse? driver, DriverError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Driver = driver;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static DriverOperationResult Success(DriverResponse driver) => new(true, driver, null, null);

    public static DriverOperationResult Failure(DriverError error, string message) => new(false, null, error, message);
}
