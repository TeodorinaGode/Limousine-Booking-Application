namespace LimousineBooking.Application.Crm;

/// <summary>Query parameters for GET /api/admin/crm/companies. Bound from the query string.</summary>
public class CrmCompanySearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches name, domain, or email (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public string? CompanyType { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public Guid? AssignedOwnerId { get; set; }

    /// <summary>True: only companies with NextFollowUpAt in the past or today. Used by the "needs follow-up" filter.</summary>
    public bool? NeedsFollowUp { get; set; }

    /// <summary>Default false — archived companies are excluded from the default list.</summary>
    public bool IncludeArchived { get; set; }

    /// <summary>One of: name, status, priority, createdAt, nextFollowUpAt. Unknown values fall back to "name".</summary>
    public string? SortBy { get; set; }

    /// <summary>"asc" (default) or "desc".</summary>
    public string? SortDirection { get; set; }

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
