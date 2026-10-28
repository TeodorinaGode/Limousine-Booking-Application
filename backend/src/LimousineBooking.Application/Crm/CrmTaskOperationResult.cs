namespace LimousineBooking.Application.Crm;

public enum CrmTaskError
{
    Validation,
    NotFound
}

public class CrmTaskOperationResult
{
    public bool Succeeded { get; }
    public CrmTaskResponse? Task { get; }
    public CrmTaskError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmTaskOperationResult(bool succeeded, CrmTaskResponse? task, CrmTaskError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Task = task;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmTaskOperationResult Success(CrmTaskResponse task) => new(true, task, null, null);

    public static CrmTaskOperationResult Failure(CrmTaskError error, string message) => new(false, null, error, message);
}
