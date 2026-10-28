using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Crm;

public class CrmAnalyticsService : ICrmAnalyticsService
{
    private readonly ICrmAnalyticsRepository _analyticsRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmEmailMessageRepository _emailMessageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CrmAnalyticsService(
        ICrmAnalyticsRepository analyticsRepository,
        ICrmActivityRepository activityRepository,
        ICrmEmailMessageRepository emailMessageRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _analyticsRepository = analyticsRepository;
        _activityRepository = activityRepository;
        _emailMessageRepository = emailMessageRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CrmDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await _analyticsRepository.GetDashboardAggregatesAsync(_dateTimeProvider.UtcNow, cancellationToken);
        var recentActivities = await _activityRepository.GetMostRecentAsync(10, cancellationToken);

        dashboard.RecentActivities = await CrmActivityResponseMapper.ToResponsesAsync(recentActivities, _emailMessageRepository, cancellationToken);

        return dashboard;
    }

    public Task<CrmReportSummaryResponse> GetReportSummaryAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default) =>
        _analyticsRepository.GetReportAggregatesAsync(range, cancellationToken);
}
