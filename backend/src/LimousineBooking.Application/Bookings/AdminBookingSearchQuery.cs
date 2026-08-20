namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Query parameters for GET /api/admin/bookings. Bound from the query string.
/// </summary>
public class AdminBookingSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches booking reference, customer first/last name, email, or phone (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    /// <summary>Comma-separated BookingStatus names (e.g. "Pending,Confirmed"). Unrecognized entries are ignored. Null/empty = no status filter.</summary>
    public string? Status { get; set; }

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    public Guid? DriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? RouteId { get; set; }

    /// <summary>One of: all (default), automatic, manual, requiresManual. Unknown values behave as "all".</summary>
    public string? AssignmentFilter { get; set; }

    /// <summary>One of: all (default), notStarted, pending, processing, paid, failed, cancelled, refunded.</summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// One of: bookingDate, pickupTime, createdAt, customerName, status.
    /// Unknown values fall back to the default (bookingDate then pickupTime,
    /// both ascending — "upcoming trips first") — never used to build raw SQL.
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
