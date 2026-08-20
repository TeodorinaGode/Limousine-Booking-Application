using LimousineBooking.Application.Common;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainRideStatusHistory = LimousineBooking.Domain.Entities.RideStatusHistory;
using DomainRoute = LimousineBooking.Domain.Entities.Route;
using DomainUser = LimousineBooking.Domain.Entities.User;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Drivers;

public class DriverBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IRideStatusHistoryRepository> _rideStatusHistoryRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ITransactionRunner> _transactionRunner = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly Guid DriverUserId = Guid.NewGuid();
    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public DriverBookingServiceTests()
    {
        _transactionRunner
            .Setup(t => t.RunSerializableAsync(It.IsAny<Func<CancellationToken, Task<DriverBookingOperationResult>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<DriverBookingOperationResult>> operation, CancellationToken ct) => operation(ct));

        _bookingRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rideStatusHistoryRepository.Setup(r => r.GetByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DomainRideStatusHistory>());
        _rideStatusHistoryRepository.Setup(r => r.AddAsync(It.IsAny<DomainRideStatusHistory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _currentUserService.Setup(c => c.UserId).Returns(DriverUserId);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private DriverBookingService CreateService() => new(
        _bookingRepository.Object,
        _driverRepository.Object,
        _rideStatusHistoryRepository.Object,
        _notificationService.Object,
        _transactionRunner.Object,
        _currentUserService.Object,
        _dateTimeProvider.Object,
        Mock.Of<ILogger<DriverBookingService>>());

    // ---- Builders ----

    private static (DomainBooking Booking, DomainRoute Route) MakeBooking(Guid driverId, DateOnly? date = null, TimeOnly? pickupTime = null)
    {
        var route = new DomainRoute("Basel", "Zurich", 90, 180.00m, "CHF");
        var booking = new DomainBooking(
            $"LM-{Random.Shared.Next(100000, 999999)}",
            "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, date ?? new DateOnly(2026, 9, 10), pickupTime ?? new TimeOnly(14, 0),
            "Bahnhofplatz 1, Basel", 2, route.Price, route.Currency);

        booking.ConfirmAutomaticAssignment(driverId, Guid.NewGuid());
        SetProperty(booking, nameof(DomainBooking.Route), route);
        return (booking, route);
    }

    private static DomainDriver MakeActiveDriver(bool driverActive = true, bool userActive = true)
    {
        var user = new DomainUser($"driver{Guid.NewGuid():N}@example.com", "hash", "Test", "Driver", UserRole.Driver);
        if (!userActive) user.Deactivate();

        var driver = new DomainDriver(user.Id, "+41791234567");
        if (!driverActive) driver.Deactivate();

        SetProperty(driver, nameof(DomainDriver.User), user);
        return driver;
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private void SetupDriverAndBooking(Guid driverId, DomainDriver driver, DomainBooking booking)
    {
        _driverRepository.Setup(r => r.GetByIdAsync(driverId, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _bookingRepository.Setup(r => r.GetByDriverAndIdAsync(driverId, booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
    }

    // ---- Dashboard ----

    [Fact]
    public async Task GetDashboardAsync_CountsTodaysAndUpcomingTrips()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var today = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(FixedUtcNow));

        var (completedToday, _) = MakeBooking(driverId, today, new TimeOnly(7, 0));
        completedToday.StartRide();
        completedToday.MarkPassengerPickedUp();
        completedToday.CompleteRide();

        var (upcomingToday, _) = MakeBooking(driverId, today, new TimeOnly(15, 0));

        _driverRepository.Setup(r => r.GetByIdAsync(driverId, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _bookingRepository.Setup(r => r.GetByDriverAndDateAsync(driverId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainBooking> { completedToday, upcomingToday });
        _bookingRepository.Setup(r => r.CountUpcomingByDriverAsync(driverId, today, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var dashboard = await CreateService().GetDashboardAsync(driverId);

        Assert.Equal(2, dashboard.TodaysTripCount);
        Assert.Equal(1, dashboard.CompletedTodayCount);
        Assert.Equal(3, dashboard.UpcomingTripCount);
        Assert.NotNull(dashboard.NextTrip);
        Assert.Equal(upcomingToday.Id, dashboard.NextTrip!.Id);
    }

    // ---- StartRide ----

    [Fact]
    public async Task StartRideAsync_FromUpcoming_MovesToOnTheWay_AndRecordsHistory()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().StartRideAsync(driverId, booking.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("OnTheWay", result.Booking!.RideStatus);
        _rideStatusHistoryRepository.Verify(r => r.AddAsync(
            It.Is<DomainRideStatusHistory>(h => h.PreviousStatus == RideStatus.Upcoming && h.NewStatus == RideStatus.OnTheWay && h.ChangedByUserId == DriverUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartRideAsync_BookingBelongsToDifferentDriver_ReturnsNotFound()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driverId, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _bookingRepository.Setup(r => r.GetByDriverAndIdAsync(driverId, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainBooking?)null);

        var result = await CreateService().StartRideAsync(driverId, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.NotFound, result.Error);
    }

    [Fact]
    public async Task StartRideAsync_InactiveDriver_ReturnsConflict()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver(driverActive: false);
        var (booking, _) = MakeBooking(driverId);
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().StartRideAsync(driverId, booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.Conflict, result.Error);
        Assert.Equal("Driver is not active.", result.ErrorMessage);
    }

    [Fact]
    public async Task StartRideAsync_AlreadyOnTheWay_ReturnsConflict_DoubleActionRejected()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        booking.StartRide();
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().StartRideAsync(driverId, booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.Conflict, result.Error);
        _rideStatusHistoryRepository.Verify(r => r.AddAsync(It.IsAny<DomainRideStatusHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- MarkPassengerPickedUp ----

    [Fact]
    public async Task MarkPassengerPickedUpAsync_FromOnTheWay_MovesToPassengerPickedUp()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        booking.StartRide();
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().MarkPassengerPickedUpAsync(driverId, booking.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("PassengerPickedUp", result.Booking!.RideStatus);
    }

    [Fact]
    public async Task MarkPassengerPickedUpAsync_BeforeRideStarted_ReturnsConflict()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().MarkPassengerPickedUpAsync(driverId, booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.Conflict, result.Error);
        Assert.Equal("The ride must be started before the passenger can be picked up.", result.ErrorMessage);
    }

    // ---- CompleteRide ----

    [Fact]
    public async Task CompleteRideAsync_FromPassengerPickedUp_CompletesBooking_AndNotifiesCustomer()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, route) = MakeBooking(driverId);
        booking.StartRide();
        booking.MarkPassengerPickedUp();
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().CompleteRideAsync(driverId, booking.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("Completed", result.Booking!.RideStatus);
        Assert.Equal("Completed", result.Booking.Status);
        _notificationService.Verify(n => n.NotifyCustomerCompletedAsync(booking, route, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRideAsync_BeforePickup_ReturnsConflict_NeverNotifies()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        booking.StartRide();
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().CompleteRideAsync(driverId, booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.Conflict, result.Error);
        _notificationService.Verify(n => n.NotifyCustomerCompletedAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteRideAsync_AlreadyCompleted_ReturnsConflict_DoubleActionRejected()
    {
        var driverId = Guid.NewGuid();
        var driver = MakeActiveDriver();
        var (booking, _) = MakeBooking(driverId);
        booking.StartRide();
        booking.MarkPassengerPickedUp();
        booking.CompleteRide();
        // A Completed booking is scoped out by GetByDriverAndIdAsync's caller in
        // practice (Status check), but the RideStatus switch is exercised directly here.
        SetupDriverAndBooking(driverId, driver, booking);

        var result = await CreateService().CompleteRideAsync(driverId, booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverBookingError.Conflict, result.Error);
        Assert.Equal("Completed bookings cannot be updated.", result.ErrorMessage);
    }
}
