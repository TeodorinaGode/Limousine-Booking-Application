using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmAnalyticsService
{
    Task<CrmDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<CrmReportSummaryResponse> GetReportSummaryAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);
}
