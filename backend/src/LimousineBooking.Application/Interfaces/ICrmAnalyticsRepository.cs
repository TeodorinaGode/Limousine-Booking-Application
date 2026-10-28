using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmAnalyticsRepository
{
    /// <summary>Current-state aggregates for the CRM Overview dashboard. RecentActivities is left empty — the service fills it via ICrmActivityRepository.</summary>
    Task<CrmDashboardResponse> GetDashboardAggregatesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<CrmReportSummaryResponse> GetReportAggregatesAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);
}
