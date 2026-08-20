using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Reports;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Reports;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    // 2026-09-15 12:00 UTC = 2026-09-15 14:00 Zurich (CEST).
    private static readonly DateTime FixedUtcNow = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ResolvedReportDateRange Range = ReportDateRangeResolver.Resolve(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15), FixedUtcNow).Range!;

    public ReportServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private ReportService CreateService() => new(_reportRepository.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task GetSummaryAsync_MapsEachFigureFromItsOwnAggregate()
    {
        _reportRepository.Setup(r => r.GetBookingCreatedAggregateAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingCreatedAggregate { Total = 10, Confirmed = 6, Pending = 2, GrossRevenue = 1800m });
        _reportRepository.Setup(r => r.GetCancelledByCancelledAtAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _reportRepository.Setup(r => r.GetCompletedByCompletionDateAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletedAggregate { Count = 4, Revenue = 720m });
        _reportRepository.Setup(r => r.GetAssignmentCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentCountAggregate { Manual = 3, Automatic = 7 });

        var summary = await CreateService().GetSummaryAsync(Range);

        Assert.Equal(10, summary.TotalBookings);
        Assert.Equal(6, summary.ConfirmedBookings);
        Assert.Equal(2, summary.PendingBookings);
        Assert.Equal(4, summary.CompletedBookings);
        Assert.Equal(2, summary.CancelledBookings);
        Assert.Equal(1800m, summary.GrossRevenue);
        Assert.Equal(720m, summary.CompletedRevenue);
        Assert.Equal(180m, summary.AverageBookingValue);
        Assert.Equal(180m, summary.AverageCompletedBookingValue);
        Assert.Equal(3, summary.ManualAssignments);
        Assert.Equal(7, summary.AutomaticAssignments);
    }

    [Fact]
    public async Task GetSummaryAsync_NoBookings_AveragesAreZero_NeverNaNOrInfinity()
    {
        _reportRepository.Setup(r => r.GetBookingCreatedAggregateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingCreatedAggregate());
        _reportRepository.Setup(r => r.GetCancelledByCancelledAtAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _reportRepository.Setup(r => r.GetCompletedByCompletionDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletedAggregate());
        _reportRepository.Setup(r => r.GetAssignmentCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentCountAggregate());

        var summary = await CreateService().GetSummaryAsync(Range);

        Assert.Equal(0m, summary.AverageBookingValue);
        Assert.Equal(0m, summary.AverageCompletedBookingValue);
    }

    [Fact]
    public async Task GetPaymentReportAsync_MapsAggregateAndKeepsPaidRevenueSeparateFromRefundedAmount()
    {
        _reportRepository.Setup(r => r.GetPaymentAggregateAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentAggregate
            {
                Total = 12,
                Successful = 6,
                Failed = 2,
                Pending = 2,
                Cancelled = 1,
                Refunded = 1,
                PaidRevenue = 1080m,
                RefundedAmount = 180m
            });

        var report = await CreateService().GetPaymentReportAsync(Range);

        Assert.Equal(12, report.TotalPaymentAttempts);
        Assert.Equal(6, report.SuccessfulPayments);
        Assert.Equal(2, report.FailedPayments);
        Assert.Equal(2, report.PendingPayments);
        Assert.Equal(1, report.CancelledPayments);
        Assert.Equal(1, report.RefundedPayments);
        Assert.Equal(1080m, report.PaidRevenue);
        Assert.Equal(180m, report.RefundedAmount);
    }

    [Fact]
    public async Task GetPopularRoutesAsync_ComputesPercentageOfTotalBookings()
    {
        _reportRepository.Setup(r => r.GetRouteAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PopularRouteItem>
            {
                new() { RouteId = Guid.NewGuid(), DepartureLocation = "Basel", Destination = "Zurich", BookingCount = 75, Revenue = 13500m },
                new() { RouteId = Guid.NewGuid(), DepartureLocation = "Basel", Destination = "Bern", BookingCount = 25, Revenue = 3750m }
            });

        var routes = await CreateService().GetPopularRoutesAsync(Range, top: null);

        Assert.Equal(75m, routes[0].PercentageOfTotalBookings);
        Assert.Equal(25m, routes[1].PercentageOfTotalBookings);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(null, 10)]
    [InlineData(0, 12)]
    public async Task GetPopularRoutesAsync_TopParameter_LimitsResults(int? top, int expectedCount)
    {
        var routes = Enumerable.Range(0, 12)
            .Select(i => new PopularRouteItem { RouteId = Guid.NewGuid(), DepartureLocation = "A", Destination = $"B{i}", BookingCount = 12 - i })
            .ToList();
        _reportRepository.Setup(r => r.GetRouteAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);

        var result = await CreateService().GetPopularRoutesAsync(Range, top);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public async Task GetDriverActivityAsync_DriverWithNoBookings_AppearsWithZeroesAndZeroCompletionRate()
    {
        var driverId = Guid.NewGuid();
        _reportRepository.Setup(r => r.GetAllDriversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverNameRow> { new() { DriverId = driverId, Name = "Idle Driver" } });
        _reportRepository.Setup(r => r.GetDriverRangeAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverRangeRow>());
        _reportRepository.Setup(r => r.GetDriverUpcomingCountsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow>());
        _reportRepository.Setup(r => r.GetDriverManualAssignmentCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow>());
        _reportRepository.Setup(r => r.GetDriverCancelledCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow>());

        var result = await CreateService().GetDriverActivityAsync(Range);

        var driver = Assert.Single(result);
        Assert.Equal("Idle Driver", driver.DriverName);
        Assert.Equal(0, driver.AssignedBookings);
        Assert.Equal(0, driver.CompletedRides);
        Assert.Equal(0m, driver.CompletionRate);
    }

    [Fact]
    public async Task GetDriverActivityAsync_ComputesCompletionRate()
    {
        var driverId = Guid.NewGuid();
        _reportRepository.Setup(r => r.GetAllDriversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverNameRow> { new() { DriverId = driverId, Name = "Busy Driver" } });
        _reportRepository.Setup(r => r.GetDriverRangeAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverRangeRow> { new() { DriverId = driverId, Assigned = 12, Completed = 10 } });
        _reportRepository.Setup(r => r.GetDriverUpcomingCountsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow> { new() { OwnerId = driverId, Count = 3 } });
        _reportRepository.Setup(r => r.GetDriverManualAssignmentCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow> { new() { OwnerId = driverId, Count = 2 } });
        _reportRepository.Setup(r => r.GetDriverCancelledCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow> { new() { OwnerId = driverId, Count = 1 } });

        var driver = Assert.Single(await CreateService().GetDriverActivityAsync(Range));

        Assert.Equal(12, driver.AssignedBookings);
        Assert.Equal(10, driver.CompletedRides);
        Assert.Equal(3, driver.UpcomingBookings);
        Assert.Equal(2, driver.ManualAssignments);
        Assert.Equal(1, driver.CancelledBookings);
        Assert.Equal(83.3m, driver.CompletionRate);
    }

    [Fact]
    public async Task GetVehicleUsageAsync_VehicleWithNoBookings_AppearsWithZeroes()
    {
        var vehicleId = Guid.NewGuid();
        _reportRepository.Setup(r => r.GetAllVehiclesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VehicleNameRow> { new() { VehicleId = vehicleId, Description = "Idle Van" } });
        _reportRepository.Setup(r => r.GetVehicleRangeAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VehicleRangeRow>());
        _reportRepository.Setup(r => r.GetVehicleUpcomingCountsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow>());

        var vehicle = Assert.Single(await CreateService().GetVehicleUsageAsync(Range));

        Assert.Equal(0, vehicle.AssignedBookings);
        Assert.Equal(0, vehicle.Utilization);
    }

    [Fact]
    public async Task GetVehicleUsageAsync_UtilizationEqualsAssignedBookingCount()
    {
        var vehicleId = Guid.NewGuid();
        _reportRepository.Setup(r => r.GetAllVehiclesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VehicleNameRow> { new() { VehicleId = vehicleId, Description = "Busy Van" } });
        _reportRepository.Setup(r => r.GetVehicleRangeAggregatesAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VehicleRangeRow> { new() { VehicleId = vehicleId, Assigned = 38, Completed = 31, Passengers = 74 } });
        _reportRepository.Setup(r => r.GetVehicleUpcomingCountsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OwnerCountRow>());

        var vehicle = Assert.Single(await CreateService().GetVehicleUsageAsync(Range));

        Assert.Equal(38, vehicle.AssignedBookings);
        Assert.Equal(38, vehicle.Utilization);
        Assert.Equal(74, vehicle.TotalPassengers);
    }

    [Fact]
    public async Task GetStatusDistributionAsync_NoBookings_PercentagesAreZero()
    {
        _reportRepository.Setup(r => r.GetStatusCountsAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var result = await CreateService().GetStatusDistributionAsync(Range);

        Assert.All(result, item => Assert.Equal(0m, item.Percentage));
    }

    [Fact]
    public async Task GetStatusDistributionAsync_ComputesPercentagesAcrossKnownStatuses()
    {
        _reportRepository.Setup(r => r.GetStatusCountsAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["Confirmed"] = 60, ["Completed"] = 25, ["Cancelled"] = 10, ["Pending"] = 5 });

        var result = await CreateService().GetStatusDistributionAsync(Range);

        Assert.Equal(60m, result.Single(r => r.Status == "Confirmed").Percentage);
        Assert.Equal(25m, result.Single(r => r.Status == "Completed").Percentage);
        Assert.Equal(10m, result.Single(r => r.Status == "Cancelled").Percentage);
        Assert.Equal(5m, result.Single(r => r.Status == "Pending").Percentage);
    }

    [Fact]
    public async Task GetAssignmentReportAsync_NoAssignments_RatesAreZero()
    {
        _reportRepository.Setup(r => r.GetAssignmentCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentCountAggregate());
        _reportRepository.Setup(r => r.GetAssignmentBaseAggregateAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentBaseAggregate());

        var result = await CreateService().GetAssignmentReportAsync(Range);

        Assert.Equal(0m, result.ManualAssignmentRate);
        Assert.Equal(0m, result.AssignmentSuccessRate);
    }

    [Fact]
    public async Task GetAssignmentReportAsync_ComputesManualAssignmentRate()
    {
        _reportRepository.Setup(r => r.GetAssignmentCountsAsync(Range.FromUtc, Range.ToUtcExclusive, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentCountAggregate { Manual = 14, Automatic = 111 });
        _reportRepository.Setup(r => r.GetAssignmentBaseAggregateAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssignmentBaseAggregate { TotalNonCancelled = 130, Assigned = 125, RequiresManual = 3 });

        var result = await CreateService().GetAssignmentReportAsync(Range);

        Assert.Equal(11.2m, result.ManualAssignmentRate);
        Assert.Equal(96.2m, result.AssignmentSuccessRate);
        Assert.Equal(3, result.RequiresManualAssignment);
    }

    [Fact]
    public async Task GetCancellationReportAsync_NoBookings_RateIsZero()
    {
        _reportRepository.Setup(r => r.GetCancelledCountInRangeAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _reportRepository.Setup(r => r.GetTotalCountInRangeAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _reportRepository.Setup(r => r.GetCancellationsByRouteAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CancellationsByRouteItem>());
        _reportRepository.Setup(r => r.GetCancellationsByDayAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CancellationsByDayItem>());
        _reportRepository.Setup(r => r.GetCancellationsByReasonAsync(Range.FromLocal, Range.ToLocal, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CancellationReasonItem>());

        var result = await CreateService().GetCancellationReportAsync(Range);

        Assert.Equal(0m, result.CancellationRate);
    }

    [Theory]
    [InlineData("today", 0)]
    [InlineData("next7", 7)]
    [InlineData(null, 7)]
    [InlineData("next30", 30)]
    public async Task GetUpcomingOperationsAsync_PeriodSelectsCorrectDateRange(string? period, int expectedDaysAhead)
    {
        // 2026-09-15 12:00 UTC = 2026-09-15 14:00 Zurich.
        var expectedToday = new DateOnly(2026, 9, 15);
        IReadOnlyList<UpcomingOperationItem>? captured = null;
        _reportRepository
            .Setup(r => r.GetUpcomingOperationsAsync(expectedToday, expectedToday.AddDays(expectedDaysAhead), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UpcomingOperationItem>())
            .Callback<DateOnly, DateOnly, CancellationToken>((_, _, _) => captured = new List<UpcomingOperationItem>());

        await CreateService().GetUpcomingOperationsAsync(period);

        _reportRepository.Verify(r => r.GetUpcomingOperationsAsync(expectedToday, expectedToday.AddDays(expectedDaysAhead), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportBookingsCsvAsync_ProducesHeaderAndOneRowPerBooking()
    {
        _reportRepository.Setup(r => r.GetBookingReportRowsAsync(Range.FromLocal, Range.ToLocal, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingReportRow>
            {
                new()
                {
                    BookingReference = "LM-000001", Date = new DateOnly(2026, 9, 5), Time = new TimeOnly(9, 0),
                    Route = "Basel - Zurich", Customer = "Jane Doe", Driver = "Dev Driver", Vehicle = "Mercedes E-Class - BS 1",
                    Passengers = 2, Price = 180m, Currency = "CHF", Status = "Confirmed", RideStatus = "Upcoming"
                }
            });

        var csv = await CreateService().ExportBookingsCsvAsync(Range);
        var lines = csv.TrimEnd('\r', '\n').Split('\n');

        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Booking Reference,Date,Time,Route,Customer,Driver,Vehicle,Passengers,Price,Currency,Status,Ride Status", lines[0]);
        Assert.Contains("LM-000001", lines[1]);
    }
}
