namespace LimousineBooking.Application.Reports;

/// <summary>Query parameters for GET /api/admin/reports/routes — the date range plus a bounded top-N (section 14).</summary>
public class RouteReportQuery : ReportDateRangeQuery
{
    /// <summary>5, 10 (default), 20, or null/0 for "all".</summary>
    public int? Top { get; set; }
}

/// <summary>One row of GET /api/admin/reports/routes, sorted by BookingCount descending (section 13).</summary>
public class PopularRouteItem
{
    /// <summary>Stable identity — grouping is by RouteId, never by name alone (section 15), so renamed routes still aggregate correctly.</summary>
    public Guid RouteId { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal PercentageOfTotalBookings { get; set; }
}
