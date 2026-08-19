namespace LimousineBooking.Application.Bookings;

/// <summary>Raw counts from IBookingRepository.GetDashboardCountsAsync — mapped into AdminDashboardResponse by AdminBookingService.</summary>
public class AdminBookingCounts
{
    public int TotalBookings { get; set; }
    public int TodaysBookings { get; set; }
    public int PendingBookings { get; set; }
    public int RequiresManualAssignmentCount { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int UpcomingTripsCount { get; set; }
}
