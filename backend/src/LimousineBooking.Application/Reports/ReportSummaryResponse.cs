namespace LimousineBooking.Application.Reports;

/// <summary>
/// GET /api/admin/reports/summary. See ReportDateRangeResolver/ReportService for
/// the exact anchor each figure uses (CreatedAt/CancelledAt/completion date/
/// AssignedAt) — they intentionally differ per section 6 of the spec.
/// </summary>
public class ReportSummaryResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }

    /// <summary>Sum of Booking.Price for all bookings created in range, regardless of cancellation (section 8).</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Sum of Booking.Price for bookings whose ride completed in range.</summary>
    public decimal CompletedRevenue { get; set; }

    public decimal AverageBookingValue { get; set; }
    public decimal AverageCompletedBookingValue { get; set; }

    public int ManualAssignments { get; set; }
    public int AutomaticAssignments { get; set; }

    /// <summary>The application currently operates in a single currency (section 10) — see Booking.Currency.</summary>
    public string Currency { get; set; } = "CHF";
}
