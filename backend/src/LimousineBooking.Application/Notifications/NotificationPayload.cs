namespace LimousineBooking.Application.Notifications;

/// <summary>
/// The fully-rendered content of one notification, JSON-serialized into
/// OutboxMessage.Payload at enqueue time. Rendering happens up front (not when
/// the worker eventually sends it) so the content reflects the booking/driver
/// state exactly as it was at the moment the event happened — important for
/// things like "the previous driver's name" on a reassignment, which won't be
/// derivable from the booking's current state by the time the worker runs.
/// </summary>
public class NotificationPayload
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>Denormalized for the admin failed-notifications list, so it doesn't need to join back to Bookings.</summary>
    public string BookingReference { get; set; } = string.Empty;
}
