namespace LimousineBooking.Application.Drivers;

/// <summary>
/// Query parameters for GET /api/admin/drivers. Bound from the query string.
/// </summary>
public class DriverSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches first name, last name, email, or phone (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>Current availability flag — distinct from IsActive (an active driver can be unavailable).</summary>
    public bool? IsAvailable { get; set; }

    /// <summary>True: only drivers with an assigned vehicle. False: only drivers without one.</summary>
    public bool? HasVehicle { get; set; }

    /// <summary>
    /// One of: firstName, lastName, email, createdAt.
    /// Unknown values fall back to "firstName" — never used to build raw SQL.
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
