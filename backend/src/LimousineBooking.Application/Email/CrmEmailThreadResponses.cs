using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Common;

namespace LimousineBooking.Application.Email;

public class CrmEmailThreadListItemResponse
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public bool HasUnread { get; set; }
    public string? LatestFromAddress { get; set; }
    public string? LatestFromName { get; set; }
    public IReadOnlyList<string> AssociatedCompanyNames { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AssociatedContactNames { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AssociatedDealNames { get; set; } = Array.Empty<string>();
}

public class CrmEmailAttachmentResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class CrmEmailMessageResponse
{
    public Guid Id { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public IReadOnlyList<EmailAddressValue> ToAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public IReadOnlyList<EmailAddressValue> CcAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public IReadOnlyList<EmailAddressValue> BccAddresses { get; set; } = Array.Empty<EmailAddressValue>();
    public string Subject { get; set; } = string.Empty;

    /// <summary>Sanitized server-side (Ganss.Xss) before it ever reaches this DTO — see IHtmlSanitizerService.</summary>
    public string? BodyHtml { get; set; }

    public string? BodyPlainText { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool IsRead { get; set; }
    public IReadOnlyList<CrmEmailAttachmentResponse> Attachments { get; set; } = Array.Empty<CrmEmailAttachmentResponse>();
}

public class CrmEmailThreadDetailResponse
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool HasUnread { get; set; }
    public IReadOnlyList<CrmEmailMessageResponse> Messages { get; set; } = Array.Empty<CrmEmailMessageResponse>();
    public IReadOnlyList<CrmCompanySummary> Companies { get; set; } = Array.Empty<CrmCompanySummary>();
    public IReadOnlyList<CrmContactSummary> Contacts { get; set; } = Array.Empty<CrmContactSummary>();
    public IReadOnlyList<CrmDealSummary> Deals { get; set; } = Array.Empty<CrmDealSummary>();
}

/// <summary>Query parameters for GET /api/admin/crm/email/threads. Always sorted by LastMessageAt descending (section 5 — "sorting by most recent activity").</summary>
public class CrmEmailThreadSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;

    /// <summary>Matches subject or sender address/name (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public Guid? CompanyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public bool? UnreadOnly { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
