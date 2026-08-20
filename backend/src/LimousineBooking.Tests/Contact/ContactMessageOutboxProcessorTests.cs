using LimousineBooking.Application.Contact;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainContactMessage = LimousineBooking.Domain.Entities.ContactMessage;

namespace LimousineBooking.Tests.Contact;

public class ContactMessageOutboxProcessorTests
{
    private readonly Mock<IContactMessageRepository> _contactMessageRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailTemplateRenderer> _renderer = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public ContactMessageOutboxProcessorTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _contactMessageRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _renderer
            .Setup(r => r.Render("ContactMessageReceived", "en", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(new RenderedEmail("New contact message", "<p>Body</p>", "Body"));
    }

    private ContactMessageOutboxProcessor CreateProcessor(string adminEmail = "ops@example.com") => new(
        _contactMessageRepository.Object,
        _emailService.Object,
        _renderer.Object,
        _dateTimeProvider.Object,
        Options.Create(new NotificationSettings { AdminEmail = adminEmail }),
        Mock.Of<ILogger<ContactMessageOutboxProcessor>>());

    private static DomainContactMessage MakeMessage() =>
        new("Jane Doe", "jane.doe@example.com", "+41791234567", "Airport transfer", "I would like to book an airport transfer.");

    [Fact]
    public async Task ProcessBatchAsync_SendsPendingMessagesToAdminEmail()
    {
        var message = MakeMessage();
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(1, count);
        _emailService.Verify(e => e.SendAsync("ops@example.com", "New contact message", "<p>Body</p>", "Body", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(LimousineBooking.Domain.Enums.ContactMessageStatus.Sent, message.Status);
    }

    [Fact]
    public async Task ProcessBatchAsync_NoAdminEmailConfigured_SkipsWithoutLosingTheMessage()
    {
        var message = MakeMessage();
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });

        var count = await CreateProcessor(adminEmail: "").ProcessBatchAsync();

        Assert.Equal(0, count);
        Assert.Equal(LimousineBooking.Domain.Enums.ContactMessageStatus.Pending, message.Status);
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_EmailServiceThrows_MarksFailedAndContinues()
    {
        var message = MakeMessage();
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(1, count);
        Assert.Equal(LimousineBooking.Domain.Enums.ContactMessageStatus.Failed, message.Status);
        Assert.Equal("SMTP unreachable", message.ErrorMessage);
    }

    [Fact]
    public async Task ProcessBatchAsync_NoPendingMessages_ReturnsZero()
    {
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DomainContactMessage>());

        var count = await CreateProcessor().ProcessBatchAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ProcessBatchAsync_IncludesPreferredContactFieldsInTheRenderedFields()
    {
        var message = new DomainContactMessage("Jane Doe", "jane.doe@example.com", "+41791234567", "Airport transfer",
            "I would like to book an airport transfer.", "Phone", new DateOnly(2026, 9, 10));
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });

        await CreateProcessor().ProcessBatchAsync();

        _renderer.Verify(r => r.Render("ContactMessageReceived", "en", It.Is<IReadOnlyDictionary<string, string>>(
            fields => fields["PreferredContactMethod"] == "Phone" && fields["PreferredDate"] == "2026-09-10")), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_NoPreferenceGiven_RendersFriendlyPlaceholders()
    {
        var message = MakeMessage();
        _contactMessageRepository.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });

        await CreateProcessor().ProcessBatchAsync();

        _renderer.Verify(r => r.Render("ContactMessageReceived", "en", It.Is<IReadOnlyDictionary<string, string>>(
            fields => fields["PreferredContactMethod"] == "(no preference)" && fields["PreferredDate"] == "(not specified)")), Times.Once);
    }
}
