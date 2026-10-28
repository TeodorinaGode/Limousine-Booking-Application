namespace LimousineBooking.Application.Crm;

/// <summary>Query parameters for GET /api/admin/crm/deals (table view). Bound from the query string.</summary>
public class CrmDealSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches deal name (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public Guid? StageId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? OwnerId { get; set; }

    /// <summary>True: only open deals (neither Won nor Lost). False: only closed (Won or Lost).</summary>
    public bool? IsOpen { get; set; }

    public bool IncludeArchived { get; set; }

    /// <summary>One of: name, value, probability, expectedCloseDate, createdAt. Unknown values fall back to "createdAt".</summary>
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
