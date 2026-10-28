using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Attachment metadata only — bytes are never persisted here, only streamed live from the provider on demand via ProviderAttachmentId (keeps this app from becoming a mailbox-wide file store).</summary>
public class CrmEmailAttachment : AuditableEntity
{
    public Guid MessageId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ProviderAttachmentId { get; private set; } = string.Empty;

    public CrmEmailMessage? Message { get; private set; }

    private CrmEmailAttachment()
    {
    }

    public CrmEmailAttachment(Guid messageId, string fileName, string? contentType, long sizeBytes, string providerAttachmentId)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId is required.", nameof(messageId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(providerAttachmentId))
            throw new ArgumentException("ProviderAttachmentId is required.", nameof(providerAttachmentId));
        if (sizeBytes < 0)
            throw new ArgumentException("SizeBytes cannot be negative.", nameof(sizeBytes));

        MessageId = messageId;
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        SizeBytes = sizeBytes;
        ProviderAttachmentId = providerAttachmentId;
    }
}
