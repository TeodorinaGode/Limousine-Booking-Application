namespace LimousineBooking.Application.Crm;

/// <summary>Query parameters for GET /api/admin/crm/contacts. Bound from the query string.</summary>
public class CrmContactSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches first name, last name, email, or job title (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public string? Status { get; set; }

    public Guid? AssignedOwnerId { get; set; }

    public Guid? CompanyId { get; set; }

    public bool? NeedsFollowUp { get; set; }

    public bool IncludeArchived { get; set; }

    /// <summary>One of: firstName, lastName, email, createdAt, nextFollowUpAt. Unknown values fall back to "firstName".</summary>
    public string? SortBy { get; set; }

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
