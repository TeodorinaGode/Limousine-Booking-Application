using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// One synchronized or sent Gmail message. Uniqueness on (ConnectionId,
/// ProviderMessageId) — enforced at the database level, see
/// CrmEmailMessageConfiguration — is the single source of truth preventing
/// duplicate rows no matter how many times a message is seen across sync
/// passes or echoed back after being sent from the CRM.
/// </summary>
public class CrmEmailMessage : AuditableEntity
{
    public Guid ThreadId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public string ProviderMessageId { get; private set; } = string.Empty;
    public EmailDirection Direction { get; private set; }
    public string FromAddress { get; private set; } = string.Empty;
    public string? FromName { get; private set; }
    public string ToAddressesJson { get; private set; } = "[]";
    public string? CcAddressesJson { get; private set; }
    public string? BccAddressesJson { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string? BodyHtml { get; private set; }
    public string? BodyPlainText { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public bool IsRead { get; private set; }

    /// <summary>Set only for messages that originated from the CRM's own composer — lets the activity-timeline insert happen exactly once per message regardless of whether it's later echoed back by sync.</summary>
    public Guid? SendOperationId { get; private set; }

    public CrmEmailThread? Thread { get; private set; }
    public ICollection<CrmEmailAttachment> Attachments { get; private set; } = new List<CrmEmailAttachment>();

    private CrmEmailMessage()
    {
    }

    public CrmEmailMessage(
        Guid threadId,
        Guid connectionId,
        string providerMessageId,
        EmailDirection direction,
        string fromAddress,
        string? fromName,
        IReadOnlyList<EmailAddressValue> toAddresses,
        IReadOnlyList<EmailAddressValue>? ccAddresses,
        IReadOnlyList<EmailAddressValue>? bccAddresses,
        string? subject,
        string? bodyHtml,
        string? bodyPlainText,
        DateTime occurredAtUtc,
        bool isRead,
        Guid? sendOperationId)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (connectionId == Guid.Empty)
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (string.IsNullOrWhiteSpace(providerMessageId))
            throw new ArgumentException("ProviderMessageId is required.", nameof(providerMessageId));
        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new ArgumentException("FromAddress is required.", nameof(fromAddress));

        ThreadId = threadId;
        ConnectionId = connectionId;
        ProviderMessageId = providerMessageId;
        Direction = direction;
        FromAddress = fromAddress;
        FromName = string.IsNullOrWhiteSpace(fromName) ? null : fromName.Trim();
        ToAddressesJson = EmailAddressListSerializer.Serialize(toAddresses);
        CcAddressesJson = ccAddresses is null or { Count: 0 } ? null : EmailAddressListSerializer.Serialize(ccAddresses);
        BccAddressesJson = bccAddresses is null or { Count: 0 } ? null : EmailAddressListSerializer.Serialize(bccAddresses);
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        BodyHtml = bodyHtml;
        BodyPlainText = bodyPlainText;
        OccurredAt = occurredAtUtc;
        IsRead = isRead;
        SendOperationId = sendOperationId;
    }

    public void MarkRead() => IsRead = true;

    public void MarkUnread() => IsRead = false;
}
