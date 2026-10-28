namespace LimousineBooking.Application.Crm;

public class CrmDealsByStageItem
{
    public string StageName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// GET /api/admin/crm/reports/summary — date-ranged analysis, using the same
/// dateFrom/dateTo convention as the booking Reports module
/// (ReportDateRangeResolver). DealsByStage is explicitly a current-state
/// pipeline snapshot, not scoped to the date range — labeled as such so it
/// isn't mistaken for a date-ranged figure.
/// </summary>
public class CrmReportSummaryResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int NewCompanies { get; set; }
    public int NewContacts { get; set; }
    public int ActivitiesLogged { get; set; }
    public int TasksCompleted { get; set; }

    /// <summary>Deals that entered a Won stage within the date range (by ClosedAt).</summary>
    public int DealsWon { get; set; }

    /// <summary>Deals that entered a Lost stage within the date range (by ClosedAt).</summary>
    public int DealsLost { get; set; }

    public decimal WonValue { get; set; }
    public string Currency { get; set; } = "CHF";

    /// <summary>
    /// DealsWon / (DealsWon + DealsLost) * 100, rounded to 1 decimal place.
    /// Null when no deals closed in range — never shown as 0%, which would
    /// misleadingly imply a known zero rather than "no data."
    /// </summary>
    public decimal? ConversionRatePercent { get; set; }

    /// <summary>Current snapshot of all open, non-archived deals grouped by stage — not scoped to DateFrom/DateTo.</summary>
    public IReadOnlyList<CrmDealsByStageItem> OpenDealsByStage { get; set; } = Array.Empty<CrmDealsByStageItem>();
}
