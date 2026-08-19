using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

public class Notification : Entity
{
    public Guid BookingId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public NotificationType NotificationType { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; private set; }

    public Booking? Booking { get; private set; }

    private Notification()
    {
    }

    public Notification(Guid bookingId, string recipient, NotificationType notificationType, NotificationChannel channel = NotificationChannel.Email)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));

        BookingId = bookingId;
        Recipient = recipient;
        NotificationType = notificationType;
        Channel = channel;
        Status = NotificationStatus.Pending;
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAtUtc;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
