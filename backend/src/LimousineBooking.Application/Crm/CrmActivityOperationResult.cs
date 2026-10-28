namespace LimousineBooking.Application.Crm;

public enum CrmActivityError
{
    Validation,
    NotFound
}

public class CrmActivityOperationResult
{
    public bool Succeeded { get; }
    public CrmActivityResponse? Activity { get; }
    public CrmActivityError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmActivityOperationResult(bool succeeded, CrmActivityResponse? activity, CrmActivityError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Activity = activity;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmActivityOperationResult Success(CrmActivityResponse activity) => new(true, activity, null, null);

    public static CrmActivityOperationResult Failure(CrmActivityError error, string message) => new(false, null, error, message);
}
