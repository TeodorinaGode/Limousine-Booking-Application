namespace LimousineBooking.Application.Crm;

/// <summary>Query parameters for GET /api/admin/crm/tasks. Bound from the query string.</summary>
public class CrmTaskSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public Guid? AssignedUserId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? ContactId { get; set; }

    public Guid? DealId { get; set; }

    /// <summary>Only tasks due on or before today, not yet completed/cancelled.</summary>
    public bool? Overdue { get; set; }

    /// <summary>Only tasks due today.</summary>
    public bool? DueToday { get; set; }

    /// <summary>One of: dueDate, priority, createdAt. Unknown values fall back to "dueDate".</summary>
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
