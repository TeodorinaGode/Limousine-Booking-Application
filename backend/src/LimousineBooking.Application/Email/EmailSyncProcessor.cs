using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Email;

/// <summary>
/// One incremental sync pass for one connection: claim the checkpoint (stale
/// runs are reclaimed, same pattern as NotificationOutboxProcessor), refresh
/// the access token if needed, fetch changed messages via Gmail's history
/// API, upsert threads/messages with dedup on ProviderMessageId, and record
/// exactly one CrmActivity per message that didn't already get one from the
/// CRM's own send flow.
/// </summary>
public class EmailSyncProcessor : IEmailSyncProcessor
{
    private readonly IEmailProviderConnectionRepository _connectionRepository;
    private readonly IEmailSyncCheckpointRepository _checkpointRepository;
    private readonly ICrmEmailThreadRepository _threadRepository;
    private readonly ICrmEmailMessageRepository _messageRepository;
    private readonly IEmailSendOperationRepository _sendOperationRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly IEmailProviderClientFactory _clientFactory;
    private readonly IEmailAccessTokenProvider _accessTokenProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly EmailSyncSettings _settings;
    private readonly ILogger<EmailSyncProcessor> _logger;

    public EmailSyncProcessor(
        IEmailProviderConnectionRepository connectionRepository,
        IEmailSyncCheckpointRepository checkpointRepository,
        ICrmEmailThreadRepository threadRepository,
        ICrmEmailMessageRepository messageRepository,
        IEmailSendOperationRepository sendOperationRepository,
        ICrmActivityRepository activityRepository,
        ICrmContactRepository contactRepository,
        IEmailProviderClientFactory clientFactory,
        IEmailAccessTokenProvider accessTokenProvider,
        IDateTimeProvider dateTimeProvider,
        IOptions<EmailSyncSettings> settings,
        ILogger<EmailSyncProcessor> logger)
    {
        _connectionRepository = connectionRepository;
        _checkpointRepository = checkpointRepository;
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _sendOperationRepository = sendOperationRepository;
        _activityRepository = activityRepository;
        _contactRepository = contactRepository;
        _clientFactory = clientFactory;
        _accessTokenProvider = accessTokenProvider;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<int> SyncAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, cancellationToken);
        if (connection is null || connection.Status == EmailConnectionStatus.Disconnected)
            return 0;

        var checkpoint = await _checkpointRepository.GetByConnectionIdAsync(connectionId, cancellationToken);
        if (checkpoint is null)
        {
            checkpoint = new EmailSyncCheckpoint(connectionId);
            await _checkpointRepository.AddAsync(checkpoint, cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!checkpoint.IsClaimable(now, _settings.StaleRunningMinutes))
            return 0;

        checkpoint.BeginRun(now);
        await _checkpointRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var client = _clientFactory.GetClient(connection.Provider);
            var accessToken = await _accessTokenProvider.GetFreshAccessTokenAsync(connection, cancellationToken);

            var page = await client.FetchIncrementalChangesAsync(accessToken, checkpoint.ProviderCursor, cancellationToken);
            if (page.CursorInvalid)
            {
                _logger.LogWarning("Email sync cursor for connection {ConnectionId} was invalid/expired — falling back to a full resync.", connectionId);
                checkpoint.InvalidateCursor();
                page = await client.FetchIncrementalChangesAsync(accessToken, null, cancellationToken);
            }

            var syncedCount = 0;
            foreach (var summary in page.ChangedMessages)
            {
                if (await _messageRepository.ExistsAsync(connectionId, summary.ProviderMessageId, cancellationToken))
                    continue;

                await SyncOneMessageAsync(connection, client, accessToken, summary.ProviderMessageId, cancellationToken);
                syncedCount++;
            }

            checkpoint.CompleteRun(_dateTimeProvider.UtcNow, page.NewCursor, syncedCount);
            connection.MarkSynced(_dateTimeProvider.UtcNow);
            await _checkpointRepository.SaveChangesAsync(cancellationToken);
            await _connectionRepository.SaveChangesAsync(cancellationToken);

            return syncedCount;
        }
        catch (EmailReauthRequiredException ex)
        {
            _logger.LogWarning(ex, "Email connection {ConnectionId} requires reauthorization.", connectionId);
            connection.MarkReauthRequired();
            checkpoint.FailRun(_dateTimeProvider.UtcNow, ex.Message);
            await _connectionRepository.SaveChangesAsync(cancellationToken);
            await _checkpointRepository.SaveChangesAsync(cancellationToken);
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Email sync failed for connection {ConnectionId}.", connectionId);
            connection.MarkSyncFailed(ex.Message);
            checkpoint.FailRun(_dateTimeProvider.UtcNow, ex.Message);
            await _connectionRepository.SaveChangesAsync(cancellationToken);
            await _checkpointRepository.SaveChangesAsync(cancellationToken);
            return 0;
        }
    }

    private async Task SyncOneMessageAsync(EmailProviderConnection connection, IEmailProviderClient client, string accessToken, string providerMessageId, CancellationToken cancellationToken)
    {
        var detail = await client.GetMessageAsync(accessToken, providerMessageId, cancellationToken);

        var thread = await _threadRepository.GetByProviderThreadIdAsync(connection.Id, detail.ProviderThreadId, cancellationToken);
        var isNewThread = thread is null;
        if (thread is null)
        {
            thread = new CrmEmailThread(connection.Id, detail.ProviderThreadId, detail.Subject, detail.OccurredAtUtc);
            await _threadRepository.AddAsync(thread, cancellationToken);
            await _threadRepository.SaveChangesAsync(cancellationToken);
        }

        var isInbound = !string.Equals(detail.FromAddress, connection.EmailAddress, StringComparison.OrdinalIgnoreCase);
        var direction = isInbound ? EmailDirection.Inbound : EmailDirection.Outbound;

        // Only auto-associate at thread creation — re-applying this on every
        // later message in an already-existing thread would silently undo a
        // user's manual unlink.
        if (isNewThread)
            await TryAutoAssociateAsync(thread, detail.FromAddress, detail.ToAddresses, cancellationToken);

        var existingSendOperation = await _sendOperationRepository.GetByProviderMessageIdAsync(detail.ProviderMessageId, cancellationToken);

        var message = new CrmEmailMessage(
            thread.Id, connection.Id, detail.ProviderMessageId, direction,
            detail.FromAddress, detail.FromName, detail.ToAddresses, detail.CcAddresses, detail.BccAddresses,
            detail.Subject, detail.BodyHtml, detail.BodyPlainText, detail.OccurredAtUtc,
            isRead: !detail.IsUnread, existingSendOperation?.Id);

        await _messageRepository.AddAsync(message, cancellationToken);

        foreach (var attachment in detail.Attachments)
        {
            await _messageRepository.AddAttachmentAsync(
                new CrmEmailAttachment(message.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.ProviderAttachmentId),
                cancellationToken);
        }

        thread.RegisterMessage(detail.OccurredAtUtc, detail.Subject, detail.IsUnread);

        await _messageRepository.SaveChangesAsync(cancellationToken);

        // The send flow (Phase 5) already creates the CrmActivity for messages
        // it originated — only create one here for messages seen for the
        // first time via sync, so a sent-then-synced-back message never
        // produces two timeline rows.
        if (existingSendOperation is null)
        {
            var activity = new CrmActivity(
                direction == EmailDirection.Inbound ? CrmActivityType.EmailReceived : CrmActivityType.EmailSent,
                detail.Subject, detail.OccurredAtUtc, connection.CreatedByUserId,
                companyId: null, contactId: null, dealId: null,
                description: null, outcome: null, emailMessageId: message.Id);
            await _activityRepository.AddAsync(activity, cancellationToken);
            await _activityRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task TryAutoAssociateAsync(CrmEmailThread thread, string fromAddress, IReadOnlyList<EmailAddressValue> toAddresses, CancellationToken cancellationToken)
    {
        var candidateAddresses = new List<string> { fromAddress };
        candidateAddresses.AddRange(toAddresses.Select(a => a.Address));

        foreach (var address in candidateAddresses.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var contact = await _contactRepository.GetByEmailAsync(address, cancellationToken);
            if (contact is null)
                continue;

            await _threadRepository.AddContactLinkAsync(new CrmEmailThreadContact(thread.Id, contact.Id), cancellationToken);

            foreach (var companyLink in contact.CompanyContacts)
            {
                await _threadRepository.AddCompanyLinkAsync(new CrmEmailThreadCompany(thread.Id, companyLink.CompanyId), cancellationToken);
            }
        }

        await _threadRepository.SaveChangesAsync(cancellationToken);
    }
}
