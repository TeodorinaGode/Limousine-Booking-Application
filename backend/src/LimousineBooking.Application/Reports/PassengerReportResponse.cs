namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/passengers.</summary>
public class PassengerReportResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int TotalPassengers { get; set; }
    public decimal AveragePassengersPerBooking { get; set; }
    public int MaximumPassengersInABooking { get; set; }
}
