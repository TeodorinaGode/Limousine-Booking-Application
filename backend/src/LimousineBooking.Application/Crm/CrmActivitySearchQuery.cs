namespace LimousineBooking.Application.Crm;

/// <summary>Query parameters for GET /api/admin/crm/activities. Bound from the query string.</summary>
public class CrmActivitySearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;

    public string? ActivityType { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? ContactId { get; set; }

    public Guid? DealId { get; set; }

    public Guid? UserId { get; set; }

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
