namespace LimousineBooking.Application.Vehicles;

public enum VehicleError
{
    Validation,
    NotFound,
    Duplicate
}

public class VehicleOperationResult
{
    public bool Succeeded { get; }
    public VehicleResponse? Vehicle { get; }
    public VehicleError? Error { get; }
    public string? ErrorMessage { get; }

    private VehicleOperationResult(bool succeeded, VehicleResponse? vehicle, VehicleError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Vehicle = vehicle;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static VehicleOperationResult Success(VehicleResponse vehicle) => new(true, vehicle, null, null);

    public static VehicleOperationResult Failure(VehicleError error, string message) => new(false, null, error, message);
}
