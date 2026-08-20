using LimousineBooking.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Notifications;

public class LoggingEmailServiceTests
{
    [Fact]
    public async Task SendAsync_NeverAttemptsExternalDelivery_AndCompletesSuccessfully()
    {
        var service = new LoggingEmailService(Mock.Of<ILogger<LoggingEmailService>>());

        var exception = await Record.ExceptionAsync(() =>
            service.SendAsync("jane.doe@example.com", "Test Subject", "<p>Body</p>", "Body"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_LogsRecipientAndSubject()
    {
        var logger = new Mock<ILogger<LoggingEmailService>>();
        var service = new LoggingEmailService(logger.Object);

        await service.SendAsync("jane.doe@example.com", "Test Subject", "<p>Body</p>", "Body");

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("jane.doe@example.com") && state.ToString()!.Contains("Test Subject")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
