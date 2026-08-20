using System.Text.Json;
using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Notifications;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public AdminNotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<PagedResult<FailedNotificationResponse>> GetFailedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _notificationRepository.SearchFailedAsync(page, pageSize, cancellationToken);

        return new PagedResult<FailedNotificationResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken);
        if (notification is null)
            return false;

        notification.ResetForRetry();
        await _notificationRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static FailedNotificationResponse ToResponse(Domain.Entities.Notification notification)
    {
        // BookingReference lives inside the rendered payload snapshot, not as a
        // separate column — deserializing here avoids a join back to Bookings.
        string bookingReference;
        try
        {
            var payload = JsonSerializer.Deserialize<NotificationPayload>(notification.Payload);
            bookingReference = payload?.BookingReference ?? string.Empty;
        }
        catch (JsonException)
        {
            bookingReference = string.Empty;
        }

        return new FailedNotificationResponse
        {
            Id = notification.Id,
            NotificationType = notification.NotificationType.ToString(),
            BookingReference = bookingReference,
            Recipient = notification.Recipient,
            CreatedAt = notification.CreatedAt,
            RetryCount = notification.RetryCount,
            LastError = notification.ErrorMessage,
            Status = notification.Status.ToString()
        };
    }
}
