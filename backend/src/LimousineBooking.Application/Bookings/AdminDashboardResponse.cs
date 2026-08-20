using LimousineBooking.Application.Notifications;

namespace LimousineBooking.Application.Bookings;

/// <summary>Operational counters + an upcoming-trips glance for the admin dashboard — not a reporting/analytics feature.</summary>
public class AdminDashboardResponse
{
    public int TotalBookings { get; set; }
    public int TodaysBookings { get; set; }
    public int PendingBookings { get; set; }
    public int RequiresManualAssignmentCount { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int UpcomingTripsCount { get; set; }

    public IReadOnlyList<UpcomingBookingItem> UpcomingBookings { get; set; } = Array.Empty<UpcomingBookingItem>();

    public OutboxSummaryCounts Notifications { get; set; } = new();
}

public class UpcomingBookingItem
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public BookingRouteSummary Route { get; set; } = new();
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? VehicleDescription { get; set; }
    public string Status { get; set; } = string.Empty;
}
