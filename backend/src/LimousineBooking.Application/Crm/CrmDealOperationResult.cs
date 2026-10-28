namespace LimousineBooking.Application.Crm;

public enum CrmDealError
{
    Validation,
    NotFound
}

public class CrmDealOperationResult
{
    public bool Succeeded { get; }
    public CrmDealResponse? Deal { get; }
    public CrmDealError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmDealOperationResult(bool succeeded, CrmDealResponse? deal, CrmDealError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Deal = deal;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmDealOperationResult Success(CrmDealResponse deal) => new(true, deal, null, null);

    public static CrmDealOperationResult Failure(CrmDealError error, string message) => new(false, null, error, message);
}
