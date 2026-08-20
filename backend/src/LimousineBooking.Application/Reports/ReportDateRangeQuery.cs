namespace LimousineBooking.Application.Reports;

/// <summary>The one date-filter shape every reporting endpoint binds from the query string (section 4).</summary>
public class ReportDateRangeQuery
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
