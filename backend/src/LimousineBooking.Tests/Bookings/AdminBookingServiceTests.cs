using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Application.Payments;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainAssignmentHistory = LimousineBooking.Domain.Entities.AssignmentHistory;
using DomainRideStatusHistory = LimousineBooking.Domain.Entities.RideStatusHistory;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainPayment = LimousineBooking.Domain.Entities.Payment;
using DomainRoute = LimousineBooking.Domain.Entities.Route;
using DomainUser = LimousineBooking.Domain.Entities.User;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Bookings;

public class AdminBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAvailabilityEvaluationService> _availabilityEvaluationService = new();
    private readonly Mock<IAutomaticAssignmentService> _automaticAssignmentService = new();
    private readonly Mock<IAssignmentHistoryRepository> _assignmentHistoryRepository = new();
    private readonly Mock<IRideStatusHistoryRepository> _rideStatusHistoryRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<ITransactionRunner> _transactionRunner = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public AdminBookingServiceTests()
    {
        _transactionRunner
            .Setup(t => t.RunSerializableAsync(It.IsAny<Func<CancellationToken, Task<AdminBookingOperationResult>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<AdminBookingOperationResult>> operation, CancellationToken ct) => operation(ct));

        _bookingRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _assignmentHistoryRepository.Setup(r => r.GetByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DomainAssignmentHistory>());
        _assignmentHistoryRepository.Setup(r => r.AddAsync(It.IsAny<DomainAssignmentHistory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rideStatusHistoryRepository.Setup(r => r.GetByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DomainRideStatusHistory>());
        _automaticAssignmentService.Setup(s => s.AssignBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationRepository.Setup(r => r.GetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new OutboxSummaryCounts());
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);
        _paymentRepository.Setup(r => r.GetByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DomainPayment>());
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _currentUserService.Setup(c => c.UserId).Returns(AdminUserId);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private AdminBookingService CreateService(BookingSettings? settings = null) => new(
        _bookingRepository.Object,
        _routeRepository.Object,
        _driverRepository.Object,
        _vehicleRepository.Object,
        _userRepository.Object,
        _availabilityEvaluationService.Object,
        _automaticAssignmentService.Object,
        _assignmentHistoryRepository.Object,
        _rideStatusHistoryRepository.Object,
        _notificationService.Object,
        _notificationRepository.Object,
        _paymentRepository.Object,
        _paymentService.Object,
        _transactionRunner.Object,
        _currentUserService.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new BookingSettings()),
        Mock.Of<ILogger<AdminBookingService>>());

    // ---- Builders ----

    private static (DomainBooking Booking, DomainRoute Route) MakeBooking(
        DateOnly? date = null, TimeOnly? pickupTime = null, int passengerCount = 2, int durationMinutes = 90, decimal price = 180.00m)
    {
        var route = new DomainRoute("Basel", "Zurich", durationMinutes, price, "CHF");
        var booking = new DomainBooking(
            $"LM-{Random.Shared.Next(100000, 999999)}",
            "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, date ?? new DateOnly(2026, 9, 10), pickupTime ?? new TimeOnly(14, 0),
            "Bahnhofplatz 1, Basel", passengerCount, route.Price, route.Currency);

        SetProperty(booking, nameof(DomainBooking.Route), route);
        return (booking, route);
    }

    private static DomainDriver MakeDriver(int vehicleCapacity = 4, bool isActive = true, bool userActive = true, bool isAvailable = true)
    {
        var user = new DomainUser($"driver{Guid.NewGuid():N}@example.com", "hash", "Test", "Driver", UserRole.Driver);
        if (!userActive) user.Deactivate();

        var vehicle = new DomainVehicle($"BS {Random.Shared.Next(100000, 999999)}", "Mercedes-Benz", "V-Class", "Van", vehicleCapacity);

        var driver = new DomainDriver(user.Id, "+41791234567");
        driver.AssignVehicle(vehicle.Id);
        if (isAvailable) driver.SetAvailable();
        if (!isActive) driver.Deactivate();

        SetProperty(driver, nameof(DomainDriver.User), user);
        SetProperty(driver, nameof(DomainDriver.CurrentVehicle), vehicle);
        return driver;
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private void SetupBooking(DomainBooking booking) =>
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

    private void AllowAssignment(DomainDriver driver, bool scheduled = true, IReadOnlyList<DomainBooking>? conflictScan = null)
    {
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _vehicleRepository.Setup(r => r.GetByIdAsync(driver.CurrentVehicleId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(driver.CurrentVehicle);
        _availabilityEvaluationService
            .Setup(a => a.IsDriverAvailableAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduled);
        _bookingRepository
            .Setup(r => r.GetConflictScanAsync(It.IsAny<DateOnly>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictScan ?? Array.Empty<DomainBooking>());
    }

    private static UpdateBookingRequest ValidUpdateRequest(DomainBooking booking, Guid? routeId = null) => new()
    {
        RouteId = routeId ?? booking.RouteId,
        BookingDate = booking.TravelDate,
        PickupTime = booking.PickupTime,
        PickupAddress = booking.PickupAddress,
        PassengerCount = booking.PassengerCount,
        CustomerFirstName = booking.CustomerFirstName,
        CustomerLastName = booking.CustomerLastName,
        CustomerEmail = booking.CustomerEmail,
        CustomerPhone = booking.CustomerPhone,
        Notes = booking.Notes
    };

    // ---- Search / details ----

    [Fact]
    public async Task SearchAsync_MapsPagedResult()
    {
        var (booking, _) = MakeBooking();
        _bookingRepository.Setup(r => r.SearchAsync(It.IsAny<AdminBookingSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { booking }, 1));

        var result = await CreateService().SearchAsync(new AdminBookingSearchQuery { Page = 1, PageSize = 20 });

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(booking.BookingReference, result.Items[0].BookingReference);
        Assert.Equal("Unassigned", result.Items[0].Assignment);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBooking_ReturnsDetail()
    {
        var (booking, route) = MakeBooking(pickupTime: new TimeOnly(14, 0), durationMinutes: 90);
        SetupBooking(booking);

        var result = await CreateService().GetByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(booking.BookingReference, result!.BookingReference);
        Assert.Equal(route.EstimatedDurationMinutes, result.EstimatedDurationMinutes);
        Assert.Equal(new TimeOnly(15, 30), result.EstimatedEndTime);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownBooking_ReturnsNull()
    {
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainBooking?)null);

        var result = await CreateService().GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ---- Edit ----

    [Fact]
    public async Task UpdateAsync_NonTripAffectingChange_DoesNotTriggerReassignment()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver();
        booking.ConfirmAutomaticAssignment(driver.Id, driver.CurrentVehicleId!.Value);
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var request = ValidUpdateRequest(booking);
        request.CustomerFirstName = "Janet";

        var result = await CreateService().UpdateAsync(booking.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal("Janet", result.Booking!.CustomerFirstName);
        Assert.Equal(BookingStatus.Confirmed.ToString(), result.Booking.Status);
        Assert.Equal(driver.Id, booking.DriverId);
        _automaticAssignmentService.Verify(s => s.AssignBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RouteChange_RecalculatesPrice()
    {
        var (booking, oldRoute) = MakeBooking(price: 180.00m);
        SetupBooking(booking);
        var newRoute = new DomainRoute("Zurich", "Geneva", 180, 450.00m, "CHF");
        _routeRepository.Setup(r => r.GetByIdAsync(newRoute.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newRoute);

        var request = ValidUpdateRequest(booking, newRoute.Id);

        var result = await CreateService().UpdateAsync(booking.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal(450.00m, result.Booking!.Price);
        Assert.Equal(newRoute.Id, result.Booking.RouteId);
    }

    [Fact]
    public async Task UpdateAsync_UnknownRoute_ReturnsNotFound()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainRoute?)null);

        var result = await CreateService().UpdateAsync(booking.Id, ValidUpdateRequest(booking, Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_InactiveRoute_IsRejected()
    {
        var (booking, route) = MakeBooking();
        route.Deactivate();
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var result = await CreateService().UpdateAsync(booking.Id, ValidUpdateRequest(booking));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Validation, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_PassengerCountExceedsMaximum_IsRejected()
    {
        var (booking, route) = MakeBooking();
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var request = ValidUpdateRequest(booking);
        request.PassengerCount = 99;

        var result = await CreateService(new BookingSettings { MaximumPassengers = 16 }).UpdateAsync(booking.Id, request);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Validation, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_InvalidCustomerEmail_IsRejected()
    {
        var (booking, route) = MakeBooking();
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var request = ValidUpdateRequest(booking);
        request.CustomerEmail = "not-an-email";

        var result = await CreateService().UpdateAsync(booking.Id, request);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Validation, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_DateChange_RevalidatesAssignment()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver();
        booking.ConfirmAutomaticAssignment(driver.Id, driver.CurrentVehicleId!.Value);
        SetupBooking(booking);
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var request = ValidUpdateRequest(booking);
        request.PickupTime = new TimeOnly(18, 0);

        await CreateService().UpdateAsync(booking.Id, request);

        // Assignment is cleared before AutomaticAssignmentService is invoked to re-decide —
        // the mock is a no-op here, so the booking is left Pending/unassigned, proving
        // UnassignForRevalidation ran rather than silently keeping the stale driver.
        Assert.Null(booking.DriverId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        _automaticAssignmentService.Verify(s => s.AssignBookingAsync(booking.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task UpdateAsync_CancelledOrCompletedBooking_IsRejected(BookingStatus status)
    {
        var (booking, _) = MakeBooking();
        if (status == BookingStatus.Cancelled)
            booking.Cancel(null, null, FixedUtcNow);
        else
            booking.ChangeStatus(BookingStatus.Completed);
        SetupBooking(booking);

        var result = await CreateService().UpdateAsync(booking.Id, ValidUpdateRequest(booking));

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    // ---- Cancellation ----

    [Fact]
    public async Task CancelAsync_PendingBooking_Succeeds()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest { Reason = "Customer requested cancellation" });

        Assert.True(result.Succeeded);
        Assert.Equal(BookingStatus.Cancelled.ToString(), result.Booking!.Status);
        Assert.Equal("Customer requested cancellation", result.Booking.CancellationReason);
        _notificationService.Verify(n => n.NotifyCustomerCancelledAsync(booking, It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ConfirmedBooking_ReleasesDriverAndVehicle()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        booking.ConfirmAutomaticAssignment(driver.Id, driver.CurrentVehicleId!.Value);
        SetupBooking(booking);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest());

        Assert.True(result.Succeeded);
        Assert.Null(booking.DriverId);
        Assert.Null(booking.VehicleId);
        Assert.Null(result.Booking!.DriverName);
    }

    [Fact]
    public async Task CancelAsync_CompletedBooking_IsRejected()
    {
        var (booking, _) = MakeBooking();
        booking.ChangeStatus(BookingStatus.Completed);
        SetupBooking(booking);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledBooking_IsRejected()
    {
        var (booking, _) = MakeBooking();
        booking.Cancel(null, null, FixedUtcNow);
        SetupBooking(booking);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task CancelAsync_OpenPaymentAttempt_IsMarkedCancelled_PaidPaymentIsUntouched()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        var openPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        openPayment.AttachCheckoutSession("cs_open", "https://checkout.example/cs_open", FixedUtcNow.AddMinutes(15));
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(openPayment);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Cancelled, openPayment.Status);
    }

    [Fact]
    public async Task CancelAsync_PaidPayment_IsNeverAutomaticallyRefundedOrCancelled()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        var paidPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        paidPayment.MarkPaid("pi_1", FixedUtcNow);
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(paidPayment);

        var result = await CreateService().CancelAsync(booking.Id, new CancelBookingRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Paid, paidPayment.Status);
        _paymentService.Verify(s => s.RefundAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Payment refund ----

    [Fact]
    public async Task RefundPaymentAsync_PaidPayment_RefundsViaProviderAndMarksRefunded()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        var paidPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        paidPayment.MarkPaid("pi_1", FixedUtcNow);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(paidPayment);
        _paymentService.Setup(s => s.RefundAsync("pi_1", booking.Price, booking.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderRefund { ProviderRefundId = "re_1", Succeeded = true });

        var result = await CreateService().RefundPaymentAsync(booking.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Refunded, paidPayment.Status);
        _paymentService.Verify(s => s.RefundAsync("pi_1", booking.Price, booking.Currency, It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefundPaymentAsync_NoPaidPayment_ReturnsConflict()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var result = await CreateService().RefundPaymentAsync(booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
        _paymentService.Verify(s => s.RefundAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundPaymentAsync_UnknownBooking_ReturnsNotFound()
    {
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainBooking?)null);

        var result = await CreateService().RefundPaymentAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.NotFound, result.Error);
    }

    // ---- Manual assignment ----

    [Fact]
    public async Task AssignDriverAsync_ValidDriver_Succeeds()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.True(result.Succeeded);
        Assert.Equal("Manual", result.Booking!.AssignmentType);
        Assert.Equal(BookingStatus.Confirmed.ToString(), result.Booking.Status);
        _assignmentHistoryRepository.Verify(r => r.AddAsync(
            It.Is<DomainAssignmentHistory>(h => h.DriverId == driver.Id && h.AssignmentType == AssignmentType.Manual && h.AssignedByUserId == AdminUserId),
            It.IsAny<CancellationToken>()), Times.Once);
        // First-time manual assignment notifies customer + driver, not the reassignment set.
        _notificationService.Verify(n => n.NotifyCustomerAssignedAsync(booking, It.IsAny<DomainRoute>(), driver, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyDriverAssignedAsync(booking, It.IsAny<DomainRoute>(), driver, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyReassignedAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<DomainDriver>(), It.IsAny<DomainDriver>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignDriverAsync_UnknownDriver_ReturnsNotFound()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _driverRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainDriver?)null);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = Guid.NewGuid(), VehicleId = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.NotFound, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_InactiveDriver_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver(isActive: false);
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_InactiveUserAccount_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver(userActive: false);
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_UnknownVehicle_ReturnsNotFound()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        SetupBooking(booking);
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _vehicleRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainVehicle?)null);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.NotFound, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_InactiveVehicle_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        driver.CurrentVehicle!.Deactivate();
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_InsufficientCapacity_IsRejected()
    {
        var (booking, _) = MakeBooking(passengerCount: 6);
        var driver = MakeDriver(vehicleCapacity: 4);
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_DriverVehicleMismatch_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        var otherVehicle = new DomainVehicle("BS 000111", "Mercedes-Benz", "S-Class", "Sedan", 4);
        SetupBooking(booking);
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _vehicleRepository.Setup(r => r.GetByIdAsync(otherVehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherVehicle);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = otherVehicle.Id });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_UnavailableDriver_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver(isAvailable: false);
        SetupBooking(booking);
        AllowAssignment(driver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    [Fact]
    public async Task AssignDriverAsync_NoMatchingSchedule_IsRejected()
    {
        var (booking, _) = MakeBooking();
        var driver = MakeDriver();
        SetupBooking(booking);
        AllowAssignment(driver, scheduled: false);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
        Assert.Contains("schedule", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignDriverAsync_DriverBookingConflict_IsRejected()
    {
        var (booking, _) = MakeBooking(pickupTime: new TimeOnly(14, 0), durationMinutes: 90);
        var driver = MakeDriver();
        var (existing, _) = MakeBooking(pickupTime: new TimeOnly(14, 30), durationMinutes: 60);
        existing.ConfirmAutomaticAssignment(driver.Id, driver.CurrentVehicleId!.Value);
        SetupBooking(booking);
        AllowAssignment(driver, conflictScan: new[] { existing });

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = driver.Id, VehicleId = driver.CurrentVehicleId!.Value });

        Assert.False(result.Succeeded);
        Assert.Contains("driver", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignDriverAsync_ReassignmentKeepsPreviousHistory()
    {
        var (booking, route) = MakeBooking();
        var oldDriver = MakeDriver();
        var newDriver = MakeDriver();
        booking.ConfirmAutomaticAssignment(oldDriver.Id, oldDriver.CurrentVehicleId!.Value);
        // GetByIdWithDetailsAsync always Includes Driver when DriverId is set — the
        // reassignment notification needs that nav property to know who to notify.
        SetProperty(booking, nameof(DomainBooking.Driver), oldDriver);
        SetupBooking(booking);
        AllowAssignment(newDriver);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = newDriver.Id, VehicleId = newDriver.CurrentVehicleId!.Value });

        Assert.True(result.Succeeded);
        Assert.Equal(newDriver.Id, booking.DriverId);
        // A new history row is added; the old one (written by AutomaticAssignmentService,
        // not this test) is never touched — AddAsync is the only write this service performs.
        _assignmentHistoryRepository.Verify(r => r.AddAsync(It.IsAny<DomainAssignmentHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyReassignedAsync(booking, route, oldDriver, newDriver, It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.NotifyCustomerAssignedAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<DomainDriver>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task AssignDriverAsync_CancelledOrCompletedBooking_IsRejected(BookingStatus status)
    {
        var (booking, _) = MakeBooking();
        if (status == BookingStatus.Cancelled)
            booking.Cancel(null, null, FixedUtcNow);
        else
            booking.ChangeStatus(BookingStatus.Completed);
        SetupBooking(booking);

        var result = await CreateService().AssignDriverAsync(booking.Id, new AssignDriverRequest { DriverId = Guid.NewGuid(), VehicleId = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
    }

    // ---- Automatic reassignment ----

    [Fact]
    public async Task AutoAssignAsync_CallsAutomaticAssignmentService()
    {
        var (booking, _) = MakeBooking();
        booking.MarkRequiresManualAssignment("No driver available.");
        SetupBooking(booking);

        var result = await CreateService().AutoAssignAsync(booking.Id);

        Assert.True(result.Succeeded);
        _automaticAssignmentService.Verify(s => s.AssignBookingAsync(booking.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoAssignAsync_CancelledBooking_IsRejected()
    {
        var (booking, _) = MakeBooking();
        booking.Cancel(null, null, FixedUtcNow);
        SetupBooking(booking);

        var result = await CreateService().AutoAssignAsync(booking.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(AdminBookingError.Conflict, result.Error);
        _automaticAssignmentService.Verify(s => s.AssignBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
