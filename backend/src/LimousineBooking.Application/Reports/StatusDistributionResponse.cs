namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/status-distribution. Population = bookings whose TravelDate falls in the selected range.</summary>
public class BookingStatusDistributionItem
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
