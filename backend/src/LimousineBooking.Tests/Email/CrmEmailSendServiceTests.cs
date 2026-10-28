using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class CrmEmailSendServiceTests
{
    private readonly Mock<IEmailProviderConnectionRepository> _connectionRepository = new();
    private readonly Mock<IEmailSendOperationRepository> _sendOperationRepository = new();
    private readonly Mock<ICrmEmailThreadRepository> _threadRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _messageRepository = new();
    private readonly Mock<ICrmActivityRepository> _activityRepository = new();
    private readonly Mock<IEmailAccessTokenProvider> _accessTokenProvider = new();
    private readonly Mock<IEmailProviderClientFactory> _clientFactory = new();
    private readonly Mock<IEmailProviderClient> _providerClient = new();

    private readonly Guid _userId = Guid.NewGuid();

    public CrmEmailSendServiceTests()
    {
        _clientFactory.Setup(f => f.GetClient(It.IsAny<EmailProvider>())).Returns(_providerClient.Object);
    }

    private CrmEmailSendService CreateService() => new(
        _connectionRepository.Object,
        _sendOperationRepository.Object,
        _threadRepository.Object,
        _messageRepository.Object,
        _activityRepository.Object,
        _accessTokenProvider.Object,
        _clientFactory.Object,
        Options.Create(new GmailOAuthSettings { OutgoingMessageIdDomain = "roi-limousinen.ch" }),
        Mock.Of<ILogger<CrmEmailSendService>>());

    private static EmailProviderConnection NewConnection() =>
        new(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted", "access-token", DateTime.UtcNow.AddHours(1), Guid.NewGuid());

    private static SendCrmEmailRequest ValidRequest() => new()
    {
        ToAddresses = new List<EmailAddressValue> { new("client@example.com", "Jane Client") },
        Subject = "Your quote",
        BodyHtml = "<p>Here is your quote.</p>"
    };

    [Fact]
    public async Task SendAsync_WhenNoConnectionExists_ReturnsNotConnected()
    {
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((EmailProviderConnection?)null);

        var result = await CreateService().SendAsync(Guid.NewGuid(), ValidRequest(), _userId);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmEmailSendError.NotConnected, result.Error);
        _providerClient.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithNoRecipients_ReturnsValidationErrorWithoutTouchingTheProvider()
    {
        var request = ValidRequest();
        request.ToAddresses = new List<EmailAddressValue>();

        var result = await CreateService().SendAsync(Guid.NewGuid(), request, _userId);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmEmailSendError.Validation, result.Error);
        _connectionRepository.Verify(r => r.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_OnTheFirstAttempt_SendsAndPersistsAConfirmedMessageAndActivity()
    {
        var connection = NewConnection();
        var clientOperationId = Guid.NewGuid();
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _sendOperationRepository.Setup(r => r.GetByClientOperationIdAsync(clientOperationId, It.IsAny<CancellationToken>())).ReturnsAsync((EmailSendOperation?)null);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync("access-token");
        _providerClient.Setup(c => c.SendMessageAsync("access-token", It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSendResult { Succeeded = true, ProviderMessageId = "provider-msg-1" });
        _providerClient.Setup(c => c.GetMessageAsync("access-token", "provider-msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderMessageDetail
            {
                ProviderMessageId = "provider-msg-1",
                ProviderThreadId = "provider-thread-1",
                FromAddress = connection.EmailAddress,
                ToAddresses = new List<EmailAddressValue> { new("client@example.com", "Jane Client") },
                Subject = "Your quote",
                BodyHtml = "<p>Here is your quote.</p>",
                OccurredAtUtc = DateTime.UtcNow
            });
        _messageRepository.Setup(r => r.GetByConnectionAndProviderMessageIdAsync(connection.Id, "provider-msg-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailMessage?)null);
        _threadRepository.Setup(r => r.GetByProviderThreadIdAsync(connection.Id, "provider-thread-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);

        var result = await CreateService().SendAsync(clientOperationId, ValidRequest(), _userId);

        Assert.True(result.Succeeded);
        Assert.Equal(EmailSendStatus.Accepted, result.Status);
        _threadRepository.Verify(r => r.AddAsync(It.IsAny<CrmEmailThread>(), It.IsAny<CancellationToken>()), Times.Once);
        _messageRepository.Verify(r => r.AddAsync(It.Is<CrmEmailMessage>(m => m.Direction == EmailDirection.Outbound), It.IsAny<CancellationToken>()), Times.Once);
        _activityRepository.Verify(r => r.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenReplayedWithTheSameClientOperationId_NeverCallsTheProviderAgain()
    {
        var connection = NewConnection();
        var clientOperationId = Guid.NewGuid();
        var operation = new EmailSendOperation(
            clientOperationId, connection.Id, "<abc@roi-limousinen.ch>",
            new List<EmailAddressValue> { new("client@example.com") }, null, null,
            "Your quote", "<p>Hi</p>", null, null, null, null, _userId);
        operation.MarkSubmitting();
        operation.MarkAccepted("provider-msg-1");

        var existingMessage = new CrmEmailMessage(
            Guid.NewGuid(), connection.Id, "provider-msg-1", EmailDirection.Outbound,
            connection.EmailAddress, null, Array.Empty<EmailAddressValue>(), null, null,
            "Your quote", "<p>Hi</p>", null, DateTime.UtcNow, isRead: true, sendOperationId: operation.Id);

        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _sendOperationRepository.Setup(r => r.GetByClientOperationIdAsync(clientOperationId, It.IsAny<CancellationToken>())).ReturnsAsync(operation);
        _messageRepository.Setup(r => r.GetBySendOperationIdAsync(operation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existingMessage);

        var result = await CreateService().SendAsync(clientOperationId, ValidRequest(), _userId);

        Assert.True(result.Succeeded);
        Assert.Equal(existingMessage.Id, result.MessageId);
        _providerClient.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _providerClient.Verify(c => c.FindMessageByMessageIdHeaderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenAPriorAttemptWasLeftSubmitting_ReconcilesBeforeEverSendingAgain()
    {
        var connection = NewConnection();
        var clientOperationId = Guid.NewGuid();
        var operation = new EmailSendOperation(
            clientOperationId, connection.Id, "<abc@roi-limousinen.ch>",
            new List<EmailAddressValue> { new("client@example.com") }, null, null,
            "Your quote", "<p>Hi</p>", null, null, null, null, _userId);
        operation.MarkSubmitting();

        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _sendOperationRepository.Setup(r => r.GetByClientOperationIdAsync(clientOperationId, It.IsAny<CancellationToken>())).ReturnsAsync(operation);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync("access-token");
        _providerClient.Setup(c => c.FindMessageByMessageIdHeaderAsync("access-token", "<abc@roi-limousinen.ch>", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderMessageDetail
            {
                ProviderMessageId = "provider-msg-1",
                ProviderThreadId = "provider-thread-1",
                FromAddress = connection.EmailAddress,
                ToAddresses = new List<EmailAddressValue> { new("client@example.com") },
                Subject = "Your quote",
                BodyHtml = "<p>Hi</p>",
                OccurredAtUtc = DateTime.UtcNow
            });
        _messageRepository.Setup(r => r.GetByConnectionAndProviderMessageIdAsync(connection.Id, "provider-msg-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailMessage?)null);
        _threadRepository.Setup(r => r.GetByProviderThreadIdAsync(connection.Id, "provider-thread-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);

        var result = await CreateService().SendAsync(clientOperationId, ValidRequest(), _userId);

        Assert.True(result.Succeeded);
        Assert.Equal(EmailSendStatus.Accepted, result.Status);
        _providerClient.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenTheProviderResultIsAmbiguous_LeavesTheOperationUnconfirmedRatherThanClaimingSuccessOrFailure()
    {
        var connection = NewConnection();
        var clientOperationId = Guid.NewGuid();
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _sendOperationRepository.Setup(r => r.GetByClientOperationIdAsync(clientOperationId, It.IsAny<CancellationToken>())).ReturnsAsync((EmailSendOperation?)null);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync("access-token");
        _providerClient.Setup(c => c.SendMessageAsync("access-token", It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSendResult { Succeeded = false, IsAmbiguous = true, ErrorMessage = "Gmail timed out." });

        var result = await CreateService().SendAsync(clientOperationId, ValidRequest(), _userId);

        Assert.False(result.Succeeded);
        Assert.Equal(EmailSendStatus.Submitting, result.Status);
        _messageRepository.Verify(r => r.AddAsync(It.IsAny<CrmEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenTheProviderRejectsTheMessage_MarksTheOperationFailed()
    {
        var connection = NewConnection();
        var clientOperationId = Guid.NewGuid();
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _sendOperationRepository.Setup(r => r.GetByClientOperationIdAsync(clientOperationId, It.IsAny<CancellationToken>())).ReturnsAsync((EmailSendOperation?)null);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync("access-token");
        _providerClient.Setup(c => c.SendMessageAsync("access-token", It.IsAny<OutgoingEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSendResult { Succeeded = false, ErrorMessage = "Invalid recipient." });

        var result = await CreateService().SendAsync(clientOperationId, ValidRequest(), _userId);

        Assert.False(result.Succeeded);
        Assert.Equal(EmailSendStatus.Failed, result.Status);
        Assert.Equal(CrmEmailSendError.ProviderError, result.Error);
    }
}
