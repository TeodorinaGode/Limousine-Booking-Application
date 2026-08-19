namespace LimousineBooking.Application.Routes;

public enum RouteError
{
    Validation,
    NotFound,
    Duplicate
}

public class RouteOperationResult
{
    public bool Succeeded { get; }
    public RouteResponse? Route { get; }
    public RouteError? Error { get; }
    public string? ErrorMessage { get; }

    private RouteOperationResult(bool succeeded, RouteResponse? route, RouteError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Route = route;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static RouteOperationResult Success(RouteResponse route) => new(true, route, null, null);

    public static RouteOperationResult Failure(RouteError error, string message) => new(false, null, error, message);
}
