namespace LimousineBooking.Application.Reports;

/// <summary>
/// GET /api/admin/reports/assignments. AutomaticAssignments/ManualAssignments come
/// from AssignmentHistory.AssignedAt in range; RequiresManualAssignment and the two
/// rates are scoped to non-cancelled bookings whose TravelDate falls in range.
/// </summary>
public class AssignmentReportResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int AutomaticAssignments { get; set; }
    public int ManualAssignments { get; set; }
    public int RequiresManualAssignment { get; set; }

    /// <summary>ManualAssignments / (ManualAssignments + AutomaticAssignments), as a percentage; 0 if there were no assignments (section 24).</summary>
    public decimal ManualAssignmentRate { get; set; }

    /// <summary>Assigned bookings / total non-cancelled bookings in range, as a percentage; 0 if there were no bookings.</summary>
    public decimal AssignmentSuccessRate { get; set; }
}
