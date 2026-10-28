using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Application.Email;

public enum CrmEmailSendError
{
    NotConnected,
    Validation,
    ProviderError
}

/// <summary>
/// Always carries the real provider-confirmed Status — the API layer must
/// never report "sent"/"delivered" just because this call returned 200.
/// Only Status == Accepted means Gmail actually confirmed receipt;
/// Submitting means the outcome is unconfirmed (retry the same
/// clientOperationId later to reconcile), and Failed means it did not go
/// out and is safe to edit and retry.
/// </summary>
public class CrmEmailSendResult
{
    public bool Succeeded { get; }
    public EmailSendStatus Status { get; }
    public Guid? MessageId { get; }
    public CrmEmailSendError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmEmailSendResult(bool succeeded, EmailSendStatus status, Guid? messageId, CrmEmailSendError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Status = status;
        MessageId = messageId;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmEmailSendResult Accepted(Guid messageId) => new(true, EmailSendStatus.Accepted, messageId, null, null);

    public static CrmEmailSendResult Unconfirmed(string errorMessage) => new(false, EmailSendStatus.Submitting, null, CrmEmailSendError.ProviderError, errorMessage);

    public static CrmEmailSendResult Failure(EmailSendStatus status, CrmEmailSendError error, string errorMessage) => new(false, status, null, error, errorMessage);
}
