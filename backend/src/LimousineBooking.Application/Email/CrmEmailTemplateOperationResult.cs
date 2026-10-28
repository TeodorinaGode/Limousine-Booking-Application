namespace LimousineBooking.Application.Email;

public enum CrmEmailTemplateError
{
    NotFound,
    Validation
}

public class CrmEmailTemplateOperationResult
{
    public bool Succeeded { get; }
    public CrmEmailTemplateResponse? Template { get; }
    public CrmEmailTemplateError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmEmailTemplateOperationResult(bool succeeded, CrmEmailTemplateResponse? template, CrmEmailTemplateError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Template = template;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmEmailTemplateOperationResult Success(CrmEmailTemplateResponse template) => new(true, template, null, null);

    public static CrmEmailTemplateOperationResult Failure(CrmEmailTemplateError error, string errorMessage) => new(false, null, error, errorMessage);
}
