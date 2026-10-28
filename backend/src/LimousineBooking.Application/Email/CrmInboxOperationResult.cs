namespace LimousineBooking.Application.Email;

public enum CrmInboxError
{
    NotFound,
    Validation,
    Duplicate
}

public class CrmInboxOperationResult
{
    public bool Succeeded { get; }
    public CrmEmailThreadDetailResponse? Thread { get; }
    public CrmInboxError? Error { get; }
    public string? ErrorMessage { get; }

    private CrmInboxOperationResult(bool succeeded, CrmEmailThreadDetailResponse? thread, CrmInboxError? error, string? errorMessage)
    {
        Succeeded = succeeded;
        Thread = thread;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CrmInboxOperationResult Success(CrmEmailThreadDetailResponse thread) => new(true, thread, null, null);

    public static CrmInboxOperationResult Failure(CrmInboxError error, string errorMessage) => new(false, null, error, errorMessage);
}

/// <summary>Attachment bytes streamed live from the provider for one download — never persisted, see CrmEmailAttachment.</summary>
public class CrmEmailAttachmentDownload
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
