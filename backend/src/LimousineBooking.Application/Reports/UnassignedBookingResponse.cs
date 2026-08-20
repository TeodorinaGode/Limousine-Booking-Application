using LimousineBooking.Application.Bookings;

namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/unassigned — currently RequiresManualAssignment=true, non-cancelled. Not date-filtered; this is a current-state view.</summary>
public class UnassignedBookingItem
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public BookingRouteSummary Route { get; set; } = new();
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
