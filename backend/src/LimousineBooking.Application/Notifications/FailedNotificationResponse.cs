namespace LimousineBooking.Application.Notifications;

public class FailedNotificationResponse
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = string.Empty;
}
