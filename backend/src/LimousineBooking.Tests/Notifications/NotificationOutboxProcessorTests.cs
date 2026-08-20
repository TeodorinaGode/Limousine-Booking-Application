using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainNotification = LimousineBooking.Domain.Entities.Notification;

namespace LimousineBooking.Tests.Notifications;

public class NotificationOutboxProcessorTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public NotificationOutboxProcessorTests()
    {
        _notificationRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private NotificationOutboxProcessor CreateProcessor(NotificationSettings? settings = null) => new(
        _notificationRepository.Object,
        _emailService.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new NotificationSettings()),
        Mock.Of<ILogger<NotificationOutboxProcessor>>());

    private static DomainNotification MakeNotification(string recipient = "jane.doe@example.com")
    {
        var payload = JsonSerializer.Serialize(new NotificationPayload
        {
            RecipientEmail = recipient,
            Subject = "Test Subject",
            HtmlBody = "<p>Body</p>",
            PlainTextBody = "Body",
            BookingReference = "LM-20261225-000123"
        });

        return new DomainNotification(Guid.NewGuid(), recipient, NotificationType.BookingConfirmation, payload);
    }

    private void SetupDue(params DomainNotification[] due) =>
        _notificationRepository
            .Setup(r => r.GetDueForProcessingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(due);

    [Fact]
    public async Task ProcessBatchAsync_SendsDueNotification_MarksSent()
    {
        var notification = MakeNotification();
        SetupDue(notification);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(1, count);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(FixedUtcNow, notification.SentAt);
        _emailService.Verify(e => e.SendAsync("jane.doe@example.com", "Test Subject", "<p>Body</p>", "Body", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_EmailServiceThrows_SchedulesRetryWithBackoff()
    {
        var notification = MakeNotification();
        SetupDue(notification);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        await CreateProcessor(new NotificationSettings { MaxRetries = 5, RetryBackoffMinutes = new[] { 1, 5, 15, 30, 60 } }).ProcessBatchAsync();

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(1, notification.RetryCount);
        Assert.Equal(FixedUtcNow.AddMinutes(1), notification.NextAttemptAt);
        Assert.Equal("SMTP unavailable", notification.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_ExhaustsMaxRetries_MarksFailed()
    {
        var notification = MakeNotification();
        // Simulate a notification already on its 4th failed attempt.
        for (var i = 0; i < 4; i++)
            notification.MarkFailed("previous failure", FixedUtcNow, maxRetries: 5, backoff: new[] { TimeSpan.FromMinutes(1) });

        SetupDue(notification);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("still down"));

        await CreateProcessor(new NotificationSettings { MaxRetries = 5, RetryBackoffMinutes = new[] { 1, 5, 15, 30, 60 } }).ProcessBatchAsync();

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(5, notification.RetryCount);
        Assert.Null(notification.NextAttemptAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_ClaimsMessageBeforeSending()
    {
        var notification = MakeNotification();
        SetupDue(notification);
        var wasProcessingWhenSendCalled = false;
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => wasProcessingWhenSendCalled = notification.Status == NotificationStatus.Processing)
            .Returns(Task.CompletedTask);

        await CreateProcessor().ProcessBatchAsync();

        Assert.True(wasProcessingWhenSendCalled);
        _notificationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessBatchAsync_NoDueMessages_ReturnsZero()
    {
        SetupDue();

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(0, count);
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleDueMessages_ProcessesAllOfThem()
    {
        var first = MakeNotification("a@example.com");
        var second = MakeNotification("b@example.com");
        SetupDue(first, second);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(2, count);
        Assert.Equal(NotificationStatus.Sent, first.Status);
        Assert.Equal(NotificationStatus.Sent, second.Status);
    }
}
