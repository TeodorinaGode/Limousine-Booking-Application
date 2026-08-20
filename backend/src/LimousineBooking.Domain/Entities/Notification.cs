using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// Doubles as this application's transactional outbox row and its notification
/// delivery log — the two were kept as one table rather than two, since a
/// dedicated NotificationLog would otherwise just duplicate everything this
/// entity already records (recipient, type, status, error, timestamps). It is
/// always written in the same SaveChangesAsync call as the business mutation
/// that caused it, so the two either both persist or both roll back together;
/// a background worker (NotificationOutboxWorker) is the only thing that ever
/// sends the actual email — nothing in the request path does.
/// </summary>
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

    /// <summary>JSON-serialized NotificationPayload (subject, html/plain-text body) — rendered once, at creation time.</summary>
    public string Payload { get; private set; } = string.Empty;

    public int RetryCount { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }

    public Booking? Booking { get; private set; }

    private Notification()
    {
    }

    public Notification(Guid bookingId, string recipient, NotificationType notificationType, string payload, NotificationChannel channel = NotificationChannel.Email)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        BookingId = bookingId;
        Recipient = recipient;
        NotificationType = notificationType;
        Payload = payload;
        Channel = channel;
        Status = NotificationStatus.Pending;
    }

    /// <summary>Claims the message for processing — saved before attempting to send, so a crash mid-send leaves a recoverable trace rather than a silently lost message.</summary>
    public void MarkProcessing(DateTime startedAt)
    {
        Status = NotificationStatus.Processing;
        ProcessingStartedAt = startedAt;
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAtUtc;
        ErrorMessage = null;
        NextAttemptAt = null;
        ProcessingStartedAt = null;
    }

    /// <summary>
    /// Increments the retry count and either schedules the next attempt (using
    /// <paramref name="backoff"/>, indexed by the new retry count) or, once
    /// <paramref name="maxRetries"/> is reached, gives up permanently.
    /// </summary>
    public void MarkFailed(string errorMessage, DateTime attemptedAt, int maxRetries, IReadOnlyList<TimeSpan> backoff)
    {
        RetryCount++;
        ErrorMessage = errorMessage;
        ProcessingStartedAt = null;

        if (RetryCount >= maxRetries)
        {
            Status = NotificationStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            Status = NotificationStatus.Pending;
            var delay = backoff[Math.Min(RetryCount - 1, backoff.Count - 1)];
            NextAttemptAt = attemptedAt + delay;
        }
    }

    /// <summary>Administrator-triggered retry: a fresh explicit attempt, so retry state starts clean rather than picking up where a permanently-failed message left off.</summary>
    public void ResetForRetry()
    {
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        ErrorMessage = null;
        NextAttemptAt = null;
        ProcessingStartedAt = null;
    }
}
