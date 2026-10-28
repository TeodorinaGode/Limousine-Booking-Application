using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// One compose-and-send attempt, keyed by a frontend-generated ClientOperationId
/// so a timed-out or ambiguous request can be safely reconciled or retried
/// without ever double-sending. GeneratedMessageIdHeader is embedded in the
/// outgoing MIME message's Message-ID header before the provider call, so an
/// ambiguous outcome can be resolved by asking the provider "did a message
/// with this exact Message-ID arrive?" before ever attempting to resend.
/// </summary>
public class EmailSendOperation : AuditableEntity
{
    public Guid ClientOperationId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public EmailSendStatus Status { get; private set; } = EmailSendStatus.Pending;
    public string GeneratedMessageIdHeader { get; private set; } = string.Empty;
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string ToAddressesJson { get; private set; } = "[]";
    public string? CcAddressesJson { get; private set; }
    public string? BccAddressesJson { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;
    public Guid? TemplateId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private EmailSendOperation()
    {
    }

    public EmailSendOperation(
        Guid clientOperationId,
        Guid connectionId,
        string generatedMessageIdHeader,
        IReadOnlyList<EmailAddressValue> toAddresses,
        IReadOnlyList<EmailAddressValue>? ccAddresses,
        IReadOnlyList<EmailAddressValue>? bccAddresses,
        string? subject,
        string bodyHtml,
        Guid? templateId,
        Guid? companyId,
        Guid? contactId,
        Guid? dealId,
        Guid createdByUserId)
    {
        if (clientOperationId == Guid.Empty)
            throw new ArgumentException("ClientOperationId is required.", nameof(clientOperationId));
        if (connectionId == Guid.Empty)
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (string.IsNullOrWhiteSpace(generatedMessageIdHeader))
            throw new ArgumentException("GeneratedMessageIdHeader is required.", nameof(generatedMessageIdHeader));
        if (toAddresses is null || toAddresses.Count == 0)
            throw new ArgumentException("At least one recipient is required.", nameof(toAddresses));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        ClientOperationId = clientOperationId;
        ConnectionId = connectionId;
        GeneratedMessageIdHeader = generatedMessageIdHeader;
        ToAddressesJson = EmailAddressListSerializer.Serialize(toAddresses);
        CcAddressesJson = ccAddresses is null or { Count: 0 } ? null : EmailAddressListSerializer.Serialize(ccAddresses);
        BccAddressesJson = bccAddresses is null or { Count: 0 } ? null : EmailAddressListSerializer.Serialize(bccAddresses);
        Subject = subject?.Trim() ?? string.Empty;
        BodyHtml = bodyHtml ?? string.Empty;
        TemplateId = templateId;
        CompanyId = companyId;
        ContactId = contactId;
        DealId = dealId;
        CreatedByUserId = createdByUserId;
    }

    public void MarkSubmitting()
    {
        if (Status != EmailSendStatus.Pending)
            throw new InvalidOperationException($"Only a Pending send operation can be marked Submitting (was {Status}).");

        Status = EmailSendStatus.Submitting;
    }

    public void MarkAccepted(string providerMessageId)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
            throw new ArgumentException("ProviderMessageId is required.", nameof(providerMessageId));

        Status = EmailSendStatus.Accepted;
        ProviderMessageId = providerMessageId;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = EmailSendStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
