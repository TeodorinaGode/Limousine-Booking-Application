using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Domain.Enums;
using Moq;
using Xunit;
using DomainNotification = LimousineBooking.Domain.Entities.Notification;

namespace LimousineBooking.Tests.Notifications;

public class AdminNotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();

    private AdminNotificationService CreateService() => new(_notificationRepository.Object);

    private static DomainNotification MakeFailedNotification(string bookingReference = "LM-20261225-000123", int retryCount = 5)
    {
        var payload = JsonSerializer.Serialize(new NotificationPayload
        {
            RecipientEmail = "jane.doe@example.com",
            Subject = "Test",
            HtmlBody = "<p>Body</p>",
            PlainTextBody = "Body",
            BookingReference = bookingReference
        });

        var notification = new DomainNotification(Guid.NewGuid(), "jane.doe@example.com", NotificationType.BookingConfirmation, payload);
        for (var i = 0; i < retryCount; i++)
            notification.MarkFailed("SMTP unavailable", DateTime.UtcNow, retryCount, new[] { TimeSpan.FromMinutes(1) });

        return notification;
    }

    [Fact]
    public async Task GetFailedAsync_MapsBookingReferenceAndRecipientFromPayload()
    {
        var notification = MakeFailedNotification();
        _notificationRepository.Setup(r => r.SearchFailedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync((new[] { notification }, 1));

        var result = await CreateService().GetFailedAsync(1, 20);

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("LM-20261225-000123", item.BookingReference);
        Assert.Equal("jane.doe@example.com", item.Recipient);
        Assert.Equal("Failed", item.Status);
        Assert.Equal(5, item.RetryCount);
        Assert.Equal("SMTP unavailable", item.LastError);
    }

    [Fact]
    public async Task RetryAsync_ExistingNotification_ResetsRetryStateAndReturnsTrue()
    {
        var notification = MakeFailedNotification();
        _notificationRepository.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);
        _notificationRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var found = await CreateService().RetryAsync(notification.Id);

        Assert.True(found);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.ErrorMessage);
        _notificationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryAsync_UnknownNotification_ReturnsFalse()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainNotification?)null);

        var found = await CreateService().RetryAsync(Guid.NewGuid());

        Assert.False(found);
        _notificationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
