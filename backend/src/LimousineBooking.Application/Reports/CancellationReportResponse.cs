namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/cancellations. Population = bookings whose TravelDate falls in the selected range.</summary>
public class CancellationReportResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int TotalCancellations { get; set; }
    public int TotalBookings { get; set; }

    /// <summary>TotalCancellations / TotalBookings as a percentage; 0 if TotalBookings is 0.</summary>
    public decimal CancellationRate { get; set; }

    public IReadOnlyList<CancellationsByRouteItem> CancellationsByRoute { get; set; } = Array.Empty<CancellationsByRouteItem>();
    public IReadOnlyList<CancellationsByDayItem> CancellationsByDay { get; set; } = Array.Empty<CancellationsByDayItem>();

    /// <summary>Raw counts per distinct reason text — empty when no cancelled booking in range recorded a reason.</summary>
    public IReadOnlyList<CancellationReasonItem> CancellationsByReason { get; set; } = Array.Empty<CancellationReasonItem>();
}

public class CancellationsByRouteItem
{
    public Guid RouteId { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CancellationsByDayItem
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class CancellationReasonItem
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}
