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

public class EmailSyncProcessorTests
{
    private readonly Mock<IEmailProviderConnectionRepository> _connectionRepository = new();
    private readonly Mock<IEmailSyncCheckpointRepository> _checkpointRepository = new();
    private readonly Mock<ICrmEmailThreadRepository> _threadRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _messageRepository = new();
    private readonly Mock<IEmailSendOperationRepository> _sendOperationRepository = new();
    private readonly Mock<ICrmActivityRepository> _activityRepository = new();
    private readonly Mock<ICrmContactRepository> _contactRepository = new();
    private readonly Mock<IEmailProviderClientFactory> _clientFactory = new();
    private readonly Mock<IEmailProviderClient> _providerClient = new();
    private readonly Mock<IEmailAccessTokenProvider> _accessTokenProvider = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    public EmailSyncProcessorTests()
    {
        _clientFactory.Setup(f => f.GetClient(It.IsAny<EmailProvider>())).Returns(_providerClient.Object);
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
    }

    private EmailSyncProcessor CreateProcessor() => new(
        _connectionRepository.Object, _checkpointRepository.Object, _threadRepository.Object, _messageRepository.Object,
        _sendOperationRepository.Object, _activityRepository.Object, _contactRepository.Object, _clientFactory.Object,
        _accessTokenProvider.Object, _dateTimeProvider.Object, Options.Create(new EmailSyncSettings { StaleRunningMinutes = 10 }),
        Mock.Of<ILogger<EmailSyncProcessor>>());

    private static EmailProviderConnection CreateFreshConnection() =>
        new(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh", "valid-access-token", Now.AddHours(1), Guid.NewGuid());

    [Fact]
    public async Task SyncAsync_WhenConnectionDoesNotExist_ReturnsZero_WithoutCallingTheProvider()
    {
        _connectionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((EmailProviderConnection?)null);

        var result = await CreateProcessor().SyncAsync(Guid.NewGuid());

        Assert.Equal(0, result);
        _providerClient.Verify(c => c.FetchIncrementalChangesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenCheckpointIsAlreadyRunningAndNotStale_ReturnsZero()
    {
        var connection = CreateFreshConnection();
        _connectionRepository.Setup(r => r.GetByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        var checkpoint = new EmailSyncCheckpoint(connection.Id);
        checkpoint.BeginRun(Now.AddMinutes(-2));
        _checkpointRepository.Setup(r => r.GetByConnectionIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(0, result);
        _providerClient.Verify(c => c.FetchIncrementalChangesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenTheAccessTokenProviderThrowsReauthRequired_MarksTheConnectionReauthRequired_AndReturnsZero()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailReauthRequiredException("revoked"));

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(0, result);
        Assert.Equal(EmailConnectionStatus.ReauthRequired, connection.Status);
    }

    [Fact]
    public async Task SyncAsync_SkipsMessagesThatAlreadyExist_WithoutFetchingTheirDetail()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection);
        _providerClient.Setup(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSyncPage
            {
                ChangedMessages = new[] { new ProviderMessageSummary { ProviderMessageId = "msg-1", ProviderThreadId = "thread-1" } },
                NewCursor = "cursor-2"
            });
        _messageRepository.Setup(r => r.ExistsAsync(connection.Id, "msg-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(0, result);
        _providerClient.Verify(c => c.GetMessageAsync(It.IsAny<string>(), "msg-1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_ForANewInboundMessage_CreatesThreadMessageAndOneActivity()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection);
        _providerClient.Setup(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSyncPage
            {
                ChangedMessages = new[] { new ProviderMessageSummary { ProviderMessageId = "msg-1", ProviderThreadId = "thread-1" } },
                NewCursor = "cursor-2"
            });
        _messageRepository.Setup(r => r.ExistsAsync(connection.Id, "msg-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _threadRepository.Setup(r => r.GetByProviderThreadIdAsync(connection.Id, "thread-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);
        _providerClient.Setup(c => c.GetMessageAsync(connection.AccessToken!, "msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderMessageDetail
            {
                ProviderMessageId = "msg-1",
                ProviderThreadId = "thread-1",
                FromAddress = "customer@example.com",
                ToAddresses = new[] { new EmailAddressValue("sales@roi-limousinen.ch") },
                Subject = "Inquiry",
                OccurredAtUtc = Now,
                IsUnread = true
            });
        _sendOperationRepository.Setup(r => r.GetByProviderMessageIdAsync("msg-1", It.IsAny<CancellationToken>())).ReturnsAsync((EmailSendOperation?)null);
        _contactRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmContact?)null);

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(1, result);
        _threadRepository.Verify(r => r.AddAsync(It.IsAny<CrmEmailThread>(), It.IsAny<CancellationToken>()), Times.Once);
        _messageRepository.Verify(r => r.AddAsync(It.Is<CrmEmailMessage>(m => m.Direction == EmailDirection.Inbound), It.IsAny<CancellationToken>()), Times.Once);
        _activityRepository.Verify(r => r.AddAsync(It.Is<CrmActivity>(a => a.ActivityType == CrmActivityType.EmailReceived), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ForAMessageThatMatchesAnExistingSendOperation_DoesNotCreateASecondActivity()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection);
        _providerClient.Setup(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSyncPage
            {
                ChangedMessages = new[] { new ProviderMessageSummary { ProviderMessageId = "msg-sent-1", ProviderThreadId = "thread-1" } },
                NewCursor = "cursor-2"
            });
        _messageRepository.Setup(r => r.ExistsAsync(connection.Id, "msg-sent-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _threadRepository.Setup(r => r.GetByProviderThreadIdAsync(connection.Id, "thread-1", It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);
        _providerClient.Setup(c => c.GetMessageAsync(connection.AccessToken!, "msg-sent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderMessageDetail
            {
                ProviderMessageId = "msg-sent-1",
                ProviderThreadId = "thread-1",
                FromAddress = connection.EmailAddress,
                ToAddresses = new[] { new EmailAddressValue("customer@example.com") },
                Subject = "Proposal",
                OccurredAtUtc = Now,
                IsUnread = false
            });
        var existingSendOperation = new EmailSendOperation(
            Guid.NewGuid(), connection.Id, "<abc@roi-limousinen.ch>",
            new[] { new EmailAddressValue("customer@example.com") }, null, null, "Proposal", "<p>Hi</p>", null, null, null, null, Guid.NewGuid());
        _sendOperationRepository.Setup(r => r.GetByProviderMessageIdAsync("msg-sent-1", It.IsAny<CancellationToken>())).ReturnsAsync(existingSendOperation);

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(1, result);
        _messageRepository.Verify(r => r.AddAsync(It.Is<CrmEmailMessage>(m => m.SendOperationId == existingSendOperation.Id), It.IsAny<CancellationToken>()), Times.Once);
        _activityRepository.Verify(r => r.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenCursorIsInvalid_InvalidatesItAndRetriesWithAFullResync()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection, cursor: "stale-cursor");

        _providerClient.SetupSequence(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSyncPage { CursorInvalid = true })
            .ReturnsAsync(new ProviderSyncPage { NewCursor = "fresh-cursor" });

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(0, result);
        _providerClient.Verify(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_OnUnexpectedFailure_MarksConnectionAndCheckpointFailed_AndReturnsZero_WithoutThrowing()
    {
        var connection = CreateFreshConnection();
        SetupHappyPathConnection(connection);
        _providerClient.Setup(c => c.FetchIncrementalChangesAsync(connection.AccessToken!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gmail returned 500."));

        var result = await CreateProcessor().SyncAsync(connection.Id);

        Assert.Equal(0, result);
        Assert.Equal(EmailConnectionStatus.Error, connection.Status);
    }

    private void SetupHappyPathConnection(EmailProviderConnection connection, string? cursor = "cursor-1")
    {
        _connectionRepository.Setup(r => r.GetByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        var checkpoint = new EmailSyncCheckpoint(connection.Id);
        if (cursor is not null)
        {
            checkpoint.BeginRun(Now.AddMinutes(-20));
            checkpoint.CompleteRun(Now.AddMinutes(-20), cursor, 0);
        }
        _checkpointRepository.Setup(r => r.GetByConnectionIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync(connection.AccessToken!);
    }
}
