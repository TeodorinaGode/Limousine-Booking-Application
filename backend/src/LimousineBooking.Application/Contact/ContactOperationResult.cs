namespace LimousineBooking.Application.Contact;

public class ContactOperationResult
{
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }

    private ContactOperationResult(bool succeeded, string? errorMessage)
    {
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public static ContactOperationResult Success() => new(true, null);

    public static ContactOperationResult Failure(string message) => new(false, message);
}
