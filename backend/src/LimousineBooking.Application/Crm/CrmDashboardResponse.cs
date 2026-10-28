namespace LimousineBooking.Application.Crm;

/// <summary>
/// GET /api/admin/crm/dashboard — a current-state snapshot (no date filter),
/// mirroring AdminHomePage's relationship to ReportsPage: this is the
/// at-a-glance view, CrmReportSummaryResponse is the date-ranged analysis.
/// Every figure is computed from real rows via direct aggregate queries —
/// never hardcoded or estimated.
/// </summary>
public class CrmDashboardResponse
{
    public int TotalCompanies { get; set; }
    public int TotalContacts { get; set; }

    /// <summary>Non-archived companies whose LastContactedAt falls within the current Europe/Zurich calendar month.</summary>
    public int CompaniesContactedThisMonth { get; set; }

    /// <summary>Non-archived companies or contacts whose NextFollowUpAt is now or in the past.</summary>
    public int ContactsAndCompaniesNeedingFollowUp { get; set; }

    public int OpenDeals { get; set; }
    public int WonDeals { get; set; }
    public int LostDeals { get; set; }

    /// <summary>Sum of Value across all open (neither Won nor Lost), non-archived deals.</summary>
    public decimal OpenPipelineValue { get; set; }

    /// <summary>Sum of Value * Probability/100 across the same open-deal set — an estimate, not a guaranteed figure.</summary>
    public decimal WeightedPipelineValue { get; set; }

    /// <summary>The currency open/weighted pipeline values are denominated in — assumes a single CRM currency, same convention as Booking.Currency elsewhere in this app.</summary>
    public string Currency { get; set; } = "CHF";

    public int TasksDueToday { get; set; }
    public int OverdueTasks { get; set; }

    /// <summary>Companies or contacts with a follow-up due in the next 7 days (inclusive of today).</summary>
    public int UpcomingFollowUps { get; set; }

    public IReadOnlyList<CrmActivityResponse> RecentActivities { get; set; } = Array.Empty<CrmActivityResponse>();
}
