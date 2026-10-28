using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Moq;
using DomainCrmActivity = LimousineBooking.Domain.Entities.CrmActivity;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Tests.Crm;

public class CrmAnalyticsServiceTests
{
    private readonly Mock<ICrmAnalyticsRepository> _analyticsRepository = new();
    private readonly Mock<ICrmActivityRepository> _activityRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _emailMessageRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public CrmAnalyticsServiceTests()
    {
        _emailMessageRepository
            .Setup(r => r.GetThreadIdsByMessageIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid>());
    }

    private CrmAnalyticsService CreateService() =>
        new(_analyticsRepository.Object, _activityRepository.Object, _emailMessageRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task GetDashboardAsync_MergesRecentActivities_FromTheActivityRepository()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(now);
        _analyticsRepository.Setup(r => r.GetDashboardAggregatesAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrmDashboardResponse { TotalCompanies = 5, OpenPipelineValue = 12345m });

        var loggedBy = new DomainUser("admin@example.com", "hash", "Admin", "User", UserRole.Administrator);
        var activity = new DomainCrmActivity(CrmActivityType.Call, "Checked in", now, loggedBy.Id, Guid.NewGuid(), null, null, null, null);
        typeof(DomainCrmActivity).GetProperty(nameof(DomainCrmActivity.User))!.SetValue(activity, loggedBy);

        _activityRepository.Setup(r => r.GetMostRecentAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { activity });

        var result = await CreateService().GetDashboardAsync();

        Assert.Equal(5, result.TotalCompanies);
        Assert.Equal(12345m, result.OpenPipelineValue);
        Assert.Single(result.RecentActivities);
        Assert.Equal("Checked in", result.RecentActivities[0].Subject);
        Assert.Equal("Admin", result.RecentActivities[0].LoggedBy.FirstName);
    }

    [Fact]
    public async Task GetReportSummaryAsync_DelegatesToTheAnalyticsRepository()
    {
        var range = new ResolvedReportDateRange
        {
            FromLocal = new DateOnly(2026, 9, 1),
            ToLocal = new DateOnly(2026, 9, 8),
            FromUtc = new DateTime(2026, 8, 31, 22, 0, 0, DateTimeKind.Utc),
            ToUtcExclusive = new DateTime(2026, 9, 8, 22, 0, 0, DateTimeKind.Utc)
        };
        var expected = new CrmReportSummaryResponse { DealsWon = 3, DealsLost = 1, ConversionRatePercent = 75m };
        _analyticsRepository.Setup(r => r.GetReportAggregatesAsync(range, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateService().GetReportSummaryAsync(range);

        Assert.Same(expected, result);
    }
}
