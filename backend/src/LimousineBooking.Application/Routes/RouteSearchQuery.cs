namespace LimousineBooking.Application.Routes;

/// <summary>
/// Query parameters for GET /api/admin/routes. Bound from the query string.
/// </summary>
public class RouteSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches DepartureLocation or Destination (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// One of: departure, destination, duration, price, status, createdAt.
    /// Unknown values fall back to "departure" — never used to build raw SQL.
    /// </summary>
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
