namespace LimousineBooking.Application.Reports;

/// <summary>GET /api/admin/reports/bookings-by-day — powers the booking-trend chart, grouped by Booking.TravelDate.</summary>
public class BookingsByDayItem
{
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
}
