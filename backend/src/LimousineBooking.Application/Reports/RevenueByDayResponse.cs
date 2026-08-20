namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/revenue-by-day — grouped by Booking.TravelDate, not CreatedAt (section 11).</summary>
public class RevenueByDayItem
{
    public DateOnly Date { get; set; }
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}
