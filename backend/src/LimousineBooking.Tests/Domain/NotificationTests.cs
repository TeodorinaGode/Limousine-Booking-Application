using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class NotificationTests
{
    private static Notification CreateValidNotification() =>
        new(Guid.NewGuid(), "jane.doe@example.com", NotificationType.BookingConfirmation, payload: "{\"RecipientEmail\":\"jane.doe@example.com\"}");

    [Fact]
    public void Notification_DefaultsToOutboxPendingState()
    {
        var notification = CreateValidNotification();

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.NextAttemptAt);
    }

    [Fact]
    public void Notification_RequiresPayload()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notification(Guid.NewGuid(), "jane.doe@example.com", NotificationType.BookingConfirmation, payload: ""));
    }

    [Fact]
    public void MarkProcessing_SetsStatusAndTimestamp()
    {
        var notification = CreateValidNotification();
        var startedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        notification.MarkProcessing(startedAt);

        Assert.Equal(NotificationStatus.Processing, notification.Status);
        Assert.Equal(startedAt, notification.ProcessingStartedAt);
    }

    [Fact]
    public void MarkSent_ClearsRetryStateAndSetsSentAt()
    {
        var notification = CreateValidNotification();
        notification.MarkFailed("transient error", DateTime.UtcNow, maxRetries: 5, backoff: new[] { TimeSpan.FromMinutes(1) });
        var sentAt = new DateTime(2026, 9, 1, 8, 5, 0, DateTimeKind.Utc);

        notification.MarkSent(sentAt);

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(sentAt, notification.SentAt);
        Assert.Null(notification.ErrorMessage);
        Assert.Null(notification.NextAttemptAt);
    }

    [Fact]
    public void MarkFailed_BelowMaxRetries_StaysPendingAndSchedulesNextAttempt()
    {
        var notification = CreateValidNotification();
        var attemptedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var backoff = new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };

        notification.MarkFailed("SMTP unavailable", attemptedAt, maxRetries: 5, backoff: backoff);

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(1, notification.RetryCount);
        Assert.Equal(attemptedAt.AddMinutes(1), notification.NextAttemptAt);
        Assert.Equal("SMTP unavailable", notification.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_UsesSuccessiveBackoffDelaysPerRetry()
    {
        var notification = CreateValidNotification();
        var backoff = new[] { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };
        var attemptedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        notification.MarkFailed("err1", attemptedAt, maxRetries: 5, backoff: backoff);
        notification.MarkFailed("err2", attemptedAt, maxRetries: 5, backoff: backoff);

        Assert.Equal(2, notification.RetryCount);
        Assert.Equal(attemptedAt.AddMinutes(5), notification.NextAttemptAt);
    }

    [Fact]
    public void MarkFailed_AtMaxRetries_BecomesPermanentlyFailed()
    {
        var notification = CreateValidNotification();
        var backoff = new[] { TimeSpan.FromMinutes(1) };
        var attemptedAt = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
            notification.MarkFailed("still failing", attemptedAt, maxRetries: 5, backoff: backoff);

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(5, notification.RetryCount);
        Assert.Null(notification.NextAttemptAt);
    }

    [Fact]
    public void ResetForRetry_ClearsRetryStateEvenAfterPermanentFailure()
    {
        var notification = CreateValidNotification();
        var backoff = new[] { TimeSpan.FromMinutes(1) };
        for (var i = 0; i < 5; i++)
            notification.MarkFailed("still failing", DateTime.UtcNow, maxRetries: 5, backoff: backoff);

        notification.ResetForRetry();

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.ErrorMessage);
        Assert.Null(notification.NextAttemptAt);
    }
}
