namespace LimousineBooking.Application.Reports;

/// <summary>
/// One row of GET /api/admin/reports/vehicles. Utilization is reported as a plain
/// booking count, not a percentage — there is no reliable "available operational
/// slots" concept in this domain model yet (section 20), so a fabricated percentage
/// would be misleading. AssignedBookings and Utilization are therefore always equal.
/// </summary>
public class VehicleUsageItem
{
    public Guid VehicleId { get; set; }
    public string VehicleDescription { get; set; } = string.Empty;

    public int AssignedBookings { get; set; }
    public int CompletedRides { get; set; }

    /// <summary>Active future bookings currently assigned to this vehicle — not scoped to the report's date range.</summary>
    public int UpcomingBookings { get; set; }

    public int TotalPassengers { get; set; }

    /// <summary>Equal to AssignedBookings for v1 — see class summary.</summary>
    public int Utilization { get; set; }
}
