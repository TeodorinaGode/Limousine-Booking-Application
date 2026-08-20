namespace LimousineBooking.Application.Drivers;

/// <summary>
/// Query parameters for GET /api/driver/bookings — the authenticated driver's own
/// schedule. No status/assignment filters like the admin search: a driver only ever
/// sees their own non-cancelled bookings, always in chronological order.
/// </summary>
public class DriverBookingSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

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
