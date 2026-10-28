namespace LimousineBooking.Application.Crm;

public enum CrmContactError
{
    Validation,
    NotFound,
    Duplicate
}

public class CrmContactOperationResult
{
    public bool Succeeded { get; }
    public CrmContactResponse? Contact { get; }
    public CrmContactError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmContactOperationResult(bool succeeded, CrmContactResponse? contact, CrmContactError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Contact = contact;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmContactOperationResult Success(CrmContactResponse contact) => new(true, contact, null, null);

    public static CrmContactOperationResult Failure(CrmContactError error, string message) => new(false, null, error, message);
}
