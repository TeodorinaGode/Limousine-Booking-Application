namespace LimousineBooking.Application.Drivers;

/// <summary>Landing view for GET /api/driver/dashboard — today's trips (Europe/Zurich "today") plus a forward-looking count.</summary>
public class DriverDashboardResponse
{
    public DateOnly Today { get; set; }
    public bool IsAvailable { get; set; }

    public int TodaysTripCount { get; set; }
    public int CompletedTodayCount { get; set; }
    public int UpcomingTripCount { get; set; }

    public IReadOnlyList<DriverBookingListItemResponse> TodaysTrips { get; set; } = Array.Empty<DriverBookingListItemResponse>();

    /// <summary>The soonest trip today that hasn't been completed yet, if any — highlighted separately in the UI.</summary>
    public DriverBookingListItemResponse? NextTrip { get; set; }
}
