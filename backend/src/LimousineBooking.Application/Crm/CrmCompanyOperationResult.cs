namespace LimousineBooking.Application.Crm;

public enum CrmCompanyError
{
    Validation,
    NotFound,
    Duplicate
}

public class CrmCompanyOperationResult
{
    public bool Succeeded { get; }
    public CrmCompanyResponse? Company { get; }
    public CrmCompanyError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmCompanyOperationResult(bool succeeded, CrmCompanyResponse? company, CrmCompanyError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Company = company;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmCompanyOperationResult Success(CrmCompanyResponse company) => new(true, company, null, null);

    public static CrmCompanyOperationResult Failure(CrmCompanyError error, string message) => new(false, null, error, message);
}
