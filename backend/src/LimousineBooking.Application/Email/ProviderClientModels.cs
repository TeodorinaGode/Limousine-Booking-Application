using LimousineBooking.Domain.Common;

namespace LimousineBooking.Application.Email;

/// <summary>
/// All the DTOs IEmailProviderClient's methods speak in — deliberately
/// provider-agnostic so a Stage 2B Microsoft Graph implementation can return
/// the exact same shapes without any CRM-facing code changing.
/// </summary>
public class EmailProviderTokenResult
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Null on a pure refresh-grant response — most providers don't reissue a refresh token every time, so the caller must keep the existing one when this is null.</summary>
    public string? RefreshToken { get; set; }

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    /// <summary>Populated only right after the initial code exchange (via the provider's own profile lookup) — null on a pure token refresh.</summary>
    public string? EmailAddress { get; set; }
}

public class ProviderMessageSummary
{
    public string ProviderMessageId { get; set; } = string.Empty;
    public string ProviderThreadId { get; set; } = string.Empty;
}

public class ProviderSyncPage
{
    public IReadOnlyList<ProviderMessageSummary> ChangedMessages { get; set; } = Array.Empty<ProviderMessageSummary>();

    /// <summary>Null when nothing changed and the existing cursor is still valid.</summary>
    public string? NewCursor { get; set; }

    /// <summary>True when the provider rejected the given cursor as expired/invalid — the caller must fall back to one full resync and start a fresh cursor.</summary>
    public bool CursorInvalid { get; set; }
}

public class ProviderAttachmentSummary
{
    public string ProviderAttachmentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class ProviderMessageDetail
{
    public string ProviderMessageId { get; set; } = string.Empty;
    public string ProviderThreadId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public IReadOnlyList<EmailAddressValue> ToAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public IReadOnlyList<EmailAddressValue> CcAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public IReadOnlyList<EmailAddressValue> BccAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public string Subject { get; set; } = string.Empty;
    public string? BodyHtml { get; set; }
    public string? BodyPlainText { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public bool IsUnread { get; set; }
    public IReadOnlyList<ProviderAttachmentSummary> Attachments { get; set; } = Array.Empty<ProviderAttachmentSummary>();
}

public class OutgoingAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class OutgoingEmailMessage
{
    public string FromAddress { get; set; } = string.Empty;
    public IReadOnlyList<EmailAddressValue> ToAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public IReadOnlyList<EmailAddressValue>? CcAddresses { get; set; }
    public IReadOnlyList<EmailAddressValue>? BccAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>The self-generated Message-ID (e.g. "&lt;{clientOperationId}@domain&gt;") embedded in the outgoing MIME message so an ambiguous send outcome can be reconciled later via FindMessageByMessageIdHeaderAsync.</summary>
    public string MessageIdHeader { get; set; } = string.Empty;

    public IReadOnlyList<OutgoingAttachment>? Attachments { get; set; }

    /// <summary>When replying within an existing synced conversation, the provider's own thread id — keeps the reply grouped in the same Gmail thread instead of starting a new one.</summary>
    public string? ProviderThreadId { get; set; }
}

public class ProviderSendResult
{
    public bool Succeeded { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>True when the HTTP call itself was ambiguous (timeout/5xx, no clean accept/reject) — tells the caller to reconcile via Message-ID lookup rather than treat this as a definite failure it's safe to retry.</summary>
    public bool IsAmbiguous { get; set; }
}
