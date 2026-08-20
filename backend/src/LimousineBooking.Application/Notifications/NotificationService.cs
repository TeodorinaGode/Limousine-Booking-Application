using System.Globalization;
using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainNotification = LimousineBooking.Domain.Entities.Notification;
using DomainPayment = LimousineBooking.Domain.Entities.Payment;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Application.Notifications;

/// <summary>
/// The one place booking/assignment services call to trigger a notification.
/// Renders the template immediately and enqueues a Notification row (this
/// application's outbox — see Notification's summary) — it never calls
/// IEmailService directly, so nothing here can fail on a provider outage.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IEmailTemplateRenderer renderer,
        IDateTimeProvider dateTimeProvider,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _renderer = renderer;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task NotifyCustomerBookingConfirmedAsync(DomainBooking booking, DomainRoute route, CancellationToken cancellationToken = default) =>
        EnqueueAsync(NotificationType.BookingConfirmation, booking, booking.CustomerEmail, "BookingConfirmed", TripFields(booking, route), cancellationToken);

    public Task NotifyCustomerBookingPendingAsync(DomainBooking booking, DomainRoute route, CancellationToken cancellationToken = default) =>
        EnqueueAsync(NotificationType.BookingPending, booking, booking.CustomerEmail, "BookingPending", TripFields(booking, route), cancellationToken);

    public Task NotifyDriverAssignedAsync(DomainBooking booking, DomainRoute route, DomainDriver driver, CancellationToken cancellationToken = default)
    {
        if (driver.User is null)
        {
            _logger.LogWarning("Skipped driver assignment notification for booking {BookingReference} — driver {DriverId} has no loaded User.", booking.BookingReference, driver.Id);
            return Task.CompletedTask;
        }

        var fields = TripFields(booking, route);
        fields["CustomerName"] = $"{booking.CustomerFirstName} {booking.CustomerLastName}";
        fields["CustomerPhone"] = booking.CustomerPhone;
        fields["Notes"] = string.IsNullOrWhiteSpace(booking.Notes) ? "(none)" : booking.Notes;

        return EnqueueAsync(NotificationType.DriverAssignment, booking, driver.User.Email, "DriverBookingAssigned", fields, cancellationToken);
    }

    public Task NotifyCustomerAssignedAsync(DomainBooking booking, DomainRoute route, DomainDriver driver, CancellationToken cancellationToken = default) =>
        EnqueueAsync(NotificationType.CustomerAssigned, booking, booking.CustomerEmail, "DriverAssigned", TripFields(booking, route), cancellationToken);

    public async Task NotifyReassignedAsync(DomainBooking booking, DomainRoute route, DomainDriver previousDriver, DomainDriver newDriver, CancellationToken cancellationToken = default)
    {
        if (previousDriver.User is not null)
        {
            await EnqueueAsync(NotificationType.DriverReassignedAway, booking, previousDriver.User.Email, "DriverReassignedAway",
                new Dictionary<string, string> { ["BookingReference"] = booking.BookingReference }, cancellationToken);
        }

        await NotifyDriverAssignedAsync(booking, route, newDriver, cancellationToken);

        await EnqueueAsync(NotificationType.BookingReassigned, booking, booking.CustomerEmail, "BookingReassigned", TripFields(booking, route), cancellationToken);
    }

    public Task NotifyCustomerCancelledAsync(DomainBooking booking, DomainRoute route, CancellationToken cancellationToken = default)
    {
        var fields = TripFields(booking, route);
        // Only a customer-appropriate reason is ever shown — never internal admin notes.
        fields["CancellationReason"] = string.IsNullOrWhiteSpace(booking.CancellationReason) ? "(no reason provided)" : booking.CancellationReason;

        return EnqueueAsync(NotificationType.BookingCancellation, booking, booking.CustomerEmail, "BookingCancelled", fields, cancellationToken);
    }

    public Task NotifyCustomerCompletedAsync(DomainBooking booking, DomainRoute route, CancellationToken cancellationToken = default) =>
        EnqueueAsync(NotificationType.RideCompleted, booking, booking.CustomerEmail, "BookingCompleted", TripFields(booking, route), cancellationToken);

    public Task NotifyAdminManualAssignmentRequiredAsync(DomainBooking booking, DomainRoute route, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            _logger.LogWarning("Skipped admin manual-assignment notification for booking {BookingReference} — NotificationSettings:AdminEmail is not configured.", booking.BookingReference);
            return Task.CompletedTask;
        }

        var fields = TripFields(booking, route);
        fields["Reason"] = reason;

        return EnqueueAsync(NotificationType.ManualAssignmentRequired, booking, _settings.AdminEmail, "AdminManualAssignmentRequired", fields, cancellationToken);
    }

    public Task ResendConfirmationAsync(DomainBooking booking, DomainRoute route, CancellationToken cancellationToken = default) =>
        EnqueueAsync(NotificationType.BookingConfirmation, booking, booking.CustomerEmail, "BookingConfirmed", TripFields(booking, route), cancellationToken);

    public Task NotifyPaymentSucceededAsync(DomainBooking booking, DomainRoute route, DomainPayment payment, CancellationToken cancellationToken = default)
    {
        var fields = TripFields(booking, route);
        fields["PaidAmount"] = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        fields["PaidCurrency"] = payment.Currency;
        fields["DriverName"] = booking.Driver?.User is null ? "To be assigned" : $"{booking.Driver.User.FirstName} {booking.Driver.User.LastName}";
        fields["VehicleDescription"] = booking.Vehicle is null ? "To be assigned" : $"{booking.Vehicle.Make} {booking.Vehicle.Model} - {booking.Vehicle.RegistrationNumber}";

        return EnqueueAsync(NotificationType.PaymentSucceeded, booking, booking.CustomerEmail, "PaymentSucceeded", fields, cancellationToken);
    }

    private async Task EnqueueAsync(
        NotificationType notificationType, DomainBooking booking, string recipientEmail, string templateName,
        Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            // The booking itself is still valid — a missing/invalid recipient
            // only means this one notification can't be delivered.
            _logger.LogWarning("Skipped {NotificationType} for booking {BookingReference} — recipient email is empty.", notificationType, booking.BookingReference);
            return;
        }

        try
        {
            var rendered = _renderer.Render(templateName, fields);

            var payload = JsonSerializer.Serialize(new NotificationPayload
            {
                RecipientEmail = recipientEmail,
                Subject = rendered.Subject,
                HtmlBody = rendered.HtmlBody,
                PlainTextBody = rendered.PlainTextBody,
                BookingReference = booking.BookingReference
            });

            var notification = new DomainNotification(booking.Id, recipientEmail, notificationType, payload);
            await _notificationRepository.AddAsync(notification, cancellationToken);

            _logger.LogInformation("Notification {NotificationType} enqueued for booking {BookingReference}.", notificationType, booking.BookingReference);
        }
        catch (Exception ex)
        {
            // Rendering/enqueueing must never take down the caller's business
            // transaction — worst case, this one notification silently doesn't happen.
            _logger.LogError(ex, "Failed to enqueue {NotificationType} notification for booking {BookingReference}.", notificationType, booking.BookingReference);
        }
    }

    private static Dictionary<string, string> TripFields(DomainBooking booking, DomainRoute route) => new()
    {
        ["CustomerName"] = $"{booking.CustomerFirstName} {booking.CustomerLastName}",
        ["BookingReference"] = booking.BookingReference,
        ["Departure"] = route.DepartureLocation,
        ["Destination"] = route.Destination,
        ["BookingDate"] = booking.TravelDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
        ["PickupTime"] = booking.PickupTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        ["PickupAddress"] = booking.PickupAddress,
        ["PassengerCount"] = booking.PassengerCount.ToString(CultureInfo.InvariantCulture),
        ["Price"] = booking.Price.ToString("0.00", CultureInfo.InvariantCulture),
        ["Currency"] = booking.Currency,
        ["Status"] = booking.Status.ToString()
    };
}
