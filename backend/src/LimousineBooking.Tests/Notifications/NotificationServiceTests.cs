using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainNotification = LimousineBooking.Domain.Entities.Notification;
using DomainRoute = LimousineBooking.Domain.Entities.Route;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Tests.Notifications;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IEmailTemplateRenderer> _renderer = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public NotificationServiceTests()
    {
        _renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new RenderedEmail("Test Subject", "<p>Body</p>", "Body"));
        _notificationRepository.Setup(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private NotificationService CreateService(NotificationSettings? settings = null) => new(
        _notificationRepository.Object,
        _renderer.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new NotificationSettings()),
        Mock.Of<ILogger<NotificationService>>());

    private static (DomainBooking Booking, DomainRoute Route) MakeBooking() =>
        (new DomainBooking("LM-20261225-000123", "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            Guid.NewGuid(), new DateOnly(2026, 12, 25), new TimeOnly(14, 0), "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF"),
         new DomainRoute("Basel", "Zurich", 60, 180.00m, "CHF"));

    private static DomainDriver MakeDriver(bool withUser = true)
    {
        var driver = new DomainDriver(Guid.NewGuid(), "+41791112233");
        if (withUser)
        {
            var user = new DomainUser($"driver{Guid.NewGuid():N}@example.com", "hash", "John", "Driver", UserRole.Driver);
            typeof(DomainDriver).GetProperty(nameof(DomainDriver.User))!.SetValue(driver, user);
        }
        return driver;
    }

    [Fact]
    public async Task NotifyCustomerBookingConfirmedAsync_EnqueuesConfirmationForCustomer()
    {
        var (booking, route) = MakeBooking();

        await CreateService().NotifyCustomerBookingConfirmedAsync(booking, route);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.BookingConfirmation && n.Recipient == "jane.doe@example.com" && n.BookingId == booking.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerBookingConfirmedAsync_RendersUsingTheBookingsLanguage()
    {
        var route = new DomainRoute("Basel", "Zurich", 60, 180.00m, "CHF");
        var booking = new DomainBooking("LM-20261225-000123", "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, new DateOnly(2026, 12, 25), new TimeOnly(14, 0), "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF", languageCode: "de");

        await CreateService().NotifyCustomerBookingConfirmedAsync(booking, route);

        _renderer.Verify(r => r.Render("BookingConfirmed", "de", It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task NotifyDriverAssignedAsync_RendersUsingTheDriversSavedLanguage_NotTheBookingsLanguage()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver();
        driver.User!.SetLanguage("fr");

        await CreateService().NotifyDriverAssignedAsync(booking, route, driver);

        _renderer.Verify(r => r.Render("DriverBookingAssigned", "fr", It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAdminManualAssignmentRequiredAsync_AlwaysRendersInEnglish()
    {
        var (booking, route) = MakeBooking();

        await CreateService(new NotificationSettings { AdminEmail = "ops@example.com" })
            .NotifyAdminManualAssignmentRequiredAsync(booking, route, "No driver available.");

        _renderer.Verify(r => r.Render("AdminManualAssignmentRequired", "en", It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerBookingPendingAsync_EnqueuesPendingForCustomer()
    {
        var (booking, route) = MakeBooking();

        await CreateService().NotifyCustomerBookingPendingAsync(booking, route);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.BookingPending && n.Recipient == booking.CustomerEmail),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyDriverAssignedAsync_UsesDriverUserEmail_NotCustomerEmail()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver();

        await CreateService().NotifyDriverAssignedAsync(booking, route, driver);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.DriverAssignment && n.Recipient == driver.User!.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyDriverAssignedAsync_DriverWithoutLoadedUser_SkipsGracefully()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver(withUser: false);

        await CreateService().NotifyDriverAssignedAsync(booking, route, driver);

        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyCustomerAssignedAsync_EnqueuesCustomerAssignedType()
    {
        var (booking, route) = MakeBooking();
        var driver = MakeDriver();

        await CreateService().NotifyCustomerAssignedAsync(booking, route, driver);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.CustomerAssigned && n.Recipient == booking.CustomerEmail),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyReassignedAsync_NotifiesPreviousDriverNewDriverAndCustomer()
    {
        var (booking, route) = MakeBooking();
        var previousDriver = MakeDriver();
        var newDriver = MakeDriver();

        await CreateService().NotifyReassignedAsync(booking, route, previousDriver, newDriver);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.DriverReassignedAway && n.Recipient == previousDriver.User!.Email),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.DriverAssignment && n.Recipient == newDriver.User!.Email),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.BookingReassigned && n.Recipient == booking.CustomerEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task NotifyCustomerCancelledAsync_IncludesCancellationReason()
    {
        var (booking, route) = MakeBooking();
        booking.Cancel("Customer requested cancellation", Guid.NewGuid(), FixedUtcNow);

        await CreateService().NotifyCustomerCancelledAsync(booking, route);

        _renderer.Verify(r => r.Render("BookingCancelled", It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(f => f["CancellationReason"] == "Customer requested cancellation")), Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerCompletedAsync_EnqueuesRideCompletedType()
    {
        var (booking, route) = MakeBooking();

        await CreateService().NotifyCustomerCompletedAsync(booking, route);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.RideCompleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAdminManualAssignmentRequiredAsync_UsesConfiguredAdminEmail()
    {
        var (booking, route) = MakeBooking();

        await CreateService(new NotificationSettings { AdminEmail = "ops@example.com" })
            .NotifyAdminManualAssignmentRequiredAsync(booking, route, "No driver available.");

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.ManualAssignmentRequired && n.Recipient == "ops@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAdminManualAssignmentRequiredAsync_NoAdminEmailConfigured_SkipsGracefully()
    {
        var (booking, route) = MakeBooking();

        await CreateService(new NotificationSettings { AdminEmail = "" })
            .NotifyAdminManualAssignmentRequiredAsync(booking, route, "No driver available.");

        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationAsync_EnqueuesConfirmationType()
    {
        var (booking, route) = MakeBooking();

        await CreateService().ResendConfirmationAsync(booking, route);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<DomainNotification>(n => n.NotificationType == NotificationType.BookingConfirmation),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_EmptyRecipient_SkipsGracefullyWithoutThrowing()
    {
        var route = new DomainRoute("Basel", "Zurich", 60, 180.00m, "CHF");
        var booking = new DomainBooking("LM-20261225-000123", "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, new DateOnly(2026, 12, 25), new TimeOnly(14, 0), "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF");

        // Admin email intentionally left unset — exercises the same "no recipient" path.
        await CreateService(new NotificationSettings { AdminEmail = "" }).NotifyAdminManualAssignmentRequiredAsync(booking, route, "reason");

        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueAsync_RendererThrows_IsCaughtAndDoesNotPropagate()
    {
        var (booking, route) = MakeBooking();
        _renderer.Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>())).Throws(new FileNotFoundException("template missing"));

        var exception = await Record.ExceptionAsync(() => CreateService().NotifyCustomerBookingConfirmedAsync(booking, route));

        Assert.Null(exception);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<DomainNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
