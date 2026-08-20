using LimousineBooking.Application.Bookings;

namespace LimousineBooking.Application.Reports;

/// <summary>Query for GET /api/admin/reports/upcoming — its own small period selector, independent of the main report date filter (section 26).</summary>
public class UpcomingOperationsQuery
{
    /// <summary>today | next7 (default) | next30.</summary>
    public string? Period { get; set; }
}

public class UpcomingOperationItem
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
    public string RideStatus { get; set; } = string.Empty;
}
