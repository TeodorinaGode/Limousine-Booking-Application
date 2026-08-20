using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainRoute = LimousineBooking.Domain.Entities.Route;
using DomainUser = LimousineBooking.Domain.Entities.User;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Bookings;

/// <summary>
/// Exercises AutomaticAssignmentService's own logic (schedule filtering, conflict/buffer
/// filtering, ranking, and the manual-assignment fallback) against mocked repositories.
/// The DB-level candidate filter (active/available/active-vehicle/capacity — all expressed
/// as a LINQ query in DriverRepository.GetAssignmentCandidatesAsync, not in this service)
/// and the real concurrency guarantee (a live PostgreSQL Serializable transaction, not
/// mockable in a meaningful way) are verified live against the real local database instead,
/// consistent with how repositories are verified elsewhere in this project.
/// </summary>
public class AutomaticAssignmentServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IAvailabilityEvaluationService> _availabilityEvaluationService = new();
    private readonly Mock<IAssignmentHistoryRepository> _assignmentHistoryRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ITransactionRunner> _transactionRunner = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public AutomaticAssignmentServiceTests()
    {
        // Pass the operation straight through — see class summary for why the real
        // Serializable-transaction retry behavior isn't exercised at this level.
        _transactionRunner
            .Setup(t => t.RunSerializableAsync(It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<bool>> operation, CancellationToken ct) => operation(ct));

        _bookingRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _assignmentHistoryRepository
            .Setup(r => r.AddAsync(It.IsAny<LimousineBooking.Domain.Entities.AssignmentHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private AutomaticAssignmentService CreateService(BookingSettings? settings = null) => new(
        _bookingRepository.Object,
        _driverRepository.Object,
        _availabilityEvaluationService.Object,
        _assignmentHistoryRepository.Object,
        _notificationService.Object,
        _transactionRunner.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new BookingSettings()),
        Mock.Of<ILogger<AutomaticAssignmentService>>());

    // ---- Builders ----

    private static (DomainBooking Booking, DomainRoute Route) MakeBooking(
        DateOnly date, TimeOnly pickupTime, int passengerCount = 2, int durationMinutes = 90)
    {
        var route = new DomainRoute("Basel", "Zurich", durationMinutes, 180.00m, "CHF");
        var booking = new DomainBooking(
            $"LM-{date:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
            "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, date, pickupTime, "Bahnhofplatz 1, Basel", passengerCount, route.Price, route.Currency);

        SetProperty(booking, nameof(DomainBooking.Route), route);
        return (booking, route);
    }

    private static DomainDriver MakeDriver(int vehicleCapacity = 4)
    {
        var user = new DomainUser($"driver{Guid.NewGuid():N}@example.com", "hash", "Test", "Driver", UserRole.Driver);
        var vehicle = new DomainVehicle($"BS {Random.Shared.Next(100000, 999999)}", "Mercedes-Benz", "V-Class", "Van", vehicleCapacity);
        var driver = new DomainDriver(user.Id, "+41791234567");
        driver.AssignVehicle(vehicle.Id);
        driver.SetAvailable();

        SetProperty(driver, nameof(DomainDriver.User), user);
        SetProperty(driver, nameof(DomainDriver.CurrentVehicle), vehicle);
        return driver;
    }

    /// <summary>An existing booking already assigned to <paramref name="driver"/>, used as a conflict-scan candidate.</summary>
    private static DomainBooking MakeExistingBooking(DomainDriver driver, DateOnly date, TimeOnly pickupTime, int durationMinutes)
    {
        var (booking, _) = MakeBooking(date, pickupTime, durationMinutes: durationMinutes);
        booking.ConfirmAutomaticAssignment(driver.Id, driver.CurrentVehicleId!.Value);
        return booking;
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private void SetupDefaults(
        DomainBooking booking,
        IReadOnlyList<DomainDriver> candidates,
        IReadOnlyList<DomainDriver>? scheduledDrivers = null,
        IReadOnlyList<DomainBooking>? conflictScan = null,
        IReadOnlyDictionary<Guid, int>? workload = null)
    {
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _driverRepository.Setup(r => r.GetAssignmentCandidatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(candidates);

        var scheduled = (scheduledDrivers ?? candidates).Select(d => d.Id).ToHashSet();
        _availabilityEvaluationService
            .Setup(a => a.IsDriverAvailableAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid driverId, DateOnly _, TimeOnly _, TimeOnly _, CancellationToken _) => scheduled.Contains(driverId));

        _bookingRepository
            .Setup(r => r.GetConflictScanAsync(It.IsAny<DateOnly>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<IReadOnlyCollection<Guid>>(), booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictScan ?? Array.Empty<DomainBooking>());

        _bookingRepository
            .Setup(r => r.GetUpcomingBookingCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workload ?? new Dictionary<Guid, int>());
    }

    // ---- Successful assignment ----

    [Fact]
    public async Task AssignBookingAsync_WithOneEligibleDriver_Succeeds()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        var driver = MakeDriver();
        SetupDefaults(booking, new[] { driver });

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(driver.Id, booking.DriverId);
        Assert.Equal(driver.CurrentVehicleId, booking.VehicleId);
        Assert.Equal(AssignmentType.Automatic, booking.AssignmentType);
        Assert.False(booking.RequiresManualAssignment);
        Assert.Null(booking.ManualAssignmentReason);
        _bookingRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignBookingAsync_OnSuccess_NotifiesCustomerConfirmedAndDriver()
    {
        var (booking, route) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        var driver = MakeDriver();
        SetupDefaults(booking, new[] { driver });

        await CreateService().AssignBookingAsync(booking.Id);

        _notificationService.Verify(n => n.NotifyCustomerBookingConfirmedAsync(booking, route, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyDriverAssignedAsync(booking, route, driver, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyCustomerBookingPendingAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- No driver available ----

    [Fact]
    public async Task AssignBookingAsync_NoCandidates_LeavesBookingPendingAndFlagsManualAssignment()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        SetupDefaults(booking, Array.Empty<DomainDriver>());

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.DriverId);
        Assert.Null(booking.VehicleId);
        Assert.True(booking.RequiresManualAssignment);
        Assert.False(string.IsNullOrWhiteSpace(booking.ManualAssignmentReason));
        _bookingRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignBookingAsync_OnFailure_NotifiesCustomerPendingAndAdmin()
    {
        var (booking, route) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        SetupDefaults(booking, Array.Empty<DomainDriver>());

        await CreateService().AssignBookingAsync(booking.Id);

        _notificationService.Verify(n => n.NotifyCustomerBookingPendingAsync(booking, route, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyAdminManualAssignmentRequiredAsync(booking, route, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyCustomerBookingConfirmedAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Outside schedule / no schedule ----

    [Fact]
    public async Task AssignBookingAsync_NoCandidateHasMatchingSchedule_RequiresManualAssignment()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        var driver = MakeDriver();
        SetupDefaults(booking, new[] { driver }, scheduledDrivers: Array.Empty<DomainDriver>());

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.True(booking.RequiresManualAssignment);
        Assert.Null(booking.DriverId);
        _bookingRepository.Verify(
            r => r.GetConflictScanAsync(It.IsAny<DateOnly>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- Booking conflict ----

    [Fact]
    public async Task AssignBookingAsync_OnlyCandidateHasOverlappingBooking_RequiresManualAssignment()
    {
        var date = new DateOnly(2026, 9, 10);
        var driver = MakeDriver();
        var existing = MakeExistingBooking(driver, date, new TimeOnly(14, 0), durationMinutes: 90); // 14:00-15:30

        var (booking, _) = MakeBooking(date, new TimeOnly(15, 0)); // 15:00-16:30 — overlaps
        SetupDefaults(booking, new[] { driver }, conflictScan: new[] { existing });

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.True(booking.RequiresManualAssignment);
        Assert.Null(booking.DriverId);
    }

    [Fact]
    public async Task AssignBookingAsync_BackToBackWithoutOverlap_IsNotTreatedAsConflict()
    {
        var date = new DateOnly(2026, 9, 10);
        var driver = MakeDriver();
        var existing = MakeExistingBooking(driver, date, new TimeOnly(14, 0), durationMinutes: 90); // ends 15:30

        // [Start, End) — a new trip starting exactly when the previous ends, with
        // no buffer configured, does not overlap.
        var (booking, _) = MakeBooking(date, new TimeOnly(15, 30));
        SetupDefaults(booking, new[] { driver }, conflictScan: new[] { existing });

        await CreateService(new BookingSettings { DriverBufferMinutes = 0 }).AssignBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(driver.Id, booking.DriverId);
    }

    // ---- Buffer ----

    [Fact]
    public async Task AssignBookingAsync_WithinBufferWindow_RequiresManualAssignment()
    {
        var date = new DateOnly(2026, 9, 10);
        var driver = MakeDriver();
        var existing = MakeExistingBooking(driver, date, new TimeOnly(14, 0), durationMinutes: 90); // ends 15:30

        // Existing ends 15:30; with a 15-minute buffer the next trip may not start before 15:45.
        var (booking, _) = MakeBooking(date, new TimeOnly(15, 40));
        SetupDefaults(booking, new[] { driver }, conflictScan: new[] { existing });

        await CreateService(new BookingSettings { DriverBufferMinutes = 15 }).AssignBookingAsync(booking.Id);

        Assert.True(booking.RequiresManualAssignment);
        Assert.Null(booking.DriverId);
    }

    [Fact]
    public async Task AssignBookingAsync_ExactlyAtBufferBoundary_Succeeds()
    {
        var date = new DateOnly(2026, 9, 10);
        var driver = MakeDriver();
        var existing = MakeExistingBooking(driver, date, new TimeOnly(14, 0), durationMinutes: 90); // ends 15:30

        var (booking, _) = MakeBooking(date, new TimeOnly(15, 45));
        SetupDefaults(booking, new[] { driver }, conflictScan: new[] { existing });

        await CreateService(new BookingSettings { DriverBufferMinutes = 15 }).AssignBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(driver.Id, booking.DriverId);
    }

    // ---- Vehicle capacity ranking ----

    [Fact]
    public async Task AssignBookingAsync_PrefersSmallestSufficientVehicle()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0), passengerCount: 2);
        var driverWithLargeVehicle = MakeDriver(vehicleCapacity: 7);
        var driverWithSmallVehicle = MakeDriver(vehicleCapacity: 3);
        SetupDefaults(booking, new[] { driverWithLargeVehicle, driverWithSmallVehicle });

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.Equal(driverWithSmallVehicle.Id, booking.DriverId);
    }

    // ---- Workload ranking ----

    [Fact]
    public async Task AssignBookingAsync_PrefersLeastBusyDriver_WhenVehicleCapacitiesAreEqual()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        var busyDriver = MakeDriver(vehicleCapacity: 4);
        var freeDriver = MakeDriver(vehicleCapacity: 4);
        var workload = new Dictionary<Guid, int> { [busyDriver.Id] = 5, [freeDriver.Id] = 1 };
        SetupDefaults(booking, new[] { busyDriver, freeDriver }, workload: workload);

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.Equal(freeDriver.Id, booking.DriverId);
    }

    // ---- Deterministic tie-break ----

    [Fact]
    public async Task AssignBookingAsync_WhenCandidatesAreOtherwiseEquivalent_PicksLowerDriverIdDeterministically()
    {
        var (booking, _) = MakeBooking(new DateOnly(2026, 9, 10), new TimeOnly(14, 0));
        var driverA = MakeDriver(vehicleCapacity: 4);
        var driverB = MakeDriver(vehicleCapacity: 4);
        SetupDefaults(booking, new[] { driverA, driverB });

        await CreateService().AssignBookingAsync(booking.Id);

        var expected = driverA.Id.CompareTo(driverB.Id) < 0 ? driverA.Id : driverB.Id;
        Assert.Equal(expected, booking.DriverId);
    }

    // ---- Pending, unassigned bookings never block ----

    [Fact]
    public async Task AssignBookingAsync_ExistingUnassignedPendingBooking_DoesNotBlockNewAssignment()
    {
        // A Pending booking with no DriverId can't appear in a real conflict scan
        // (the repository query matches on DriverId/VehicleId), but this confirms
        // the service doesn't need any extra logic to handle one if it did.
        var date = new DateOnly(2026, 9, 10);
        var driver = MakeDriver();
        var (unrelatedPendingBooking, _) = MakeBooking(date, new TimeOnly(14, 0));

        var (booking, _) = MakeBooking(date, new TimeOnly(14, 0));
        SetupDefaults(booking, new[] { driver }, conflictScan: new[] { unrelatedPendingBooking });

        await CreateService().AssignBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(driver.Id, booking.DriverId);
    }
}
