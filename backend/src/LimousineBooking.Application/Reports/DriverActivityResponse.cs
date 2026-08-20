namespace LimousineBooking.Application.Reports;

/// <summary>One row of GET /api/admin/reports/drivers. See ReportService for exactly how each figure is scoped.</summary>
public class DriverActivityItem
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;

    /// <summary>Bookings currently assigned to this driver whose travel date falls in the selected range.</summary>
    public int AssignedBookings { get; set; }

    /// <summary>Of those, how many completed.</summary>
    public int CompletedRides { get; set; }

    /// <summary>Cancelled bookings this driver was ever assigned to (via AssignmentHistory) whose cancellation date falls in range.</summary>
    public int CancelledBookings { get; set; }

    /// <summary>Active future bookings currently assigned to this driver — not scoped to the report's date range (section 17).</summary>
    public int UpcomingBookings { get; set; }

    /// <summary>From AssignmentHistory: manual assignments to this driver in range.</summary>
    public int ManualAssignments { get; set; }

    /// <summary>CompletedRides / AssignedBookings as a percentage; 0 (never NaN/Infinity) when AssignedBookings is 0.</summary>
    public decimal CompletionRate { get; set; }
}
