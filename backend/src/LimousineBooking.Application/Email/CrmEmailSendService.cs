using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Email;

public class CrmEmailSendService : ICrmEmailSendService
{
    private readonly IEmailProviderConnectionRepository _connectionRepository;
    private readonly IEmailSendOperationRepository _sendOperationRepository;
    private readonly ICrmEmailThreadRepository _threadRepository;
    private readonly ICrmEmailMessageRepository _messageRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly IEmailAccessTokenProvider _accessTokenProvider;
    private readonly IEmailProviderClientFactory _clientFactory;
    private readonly GmailOAuthSettings _settings;
    private readonly ILogger<CrmEmailSendService> _logger;

    public CrmEmailSendService(
        IEmailProviderConnectionRepository connectionRepository,
        IEmailSendOperationRepository sendOperationRepository,
        ICrmEmailThreadRepository threadRepository,
        ICrmEmailMessageRepository messageRepository,
        ICrmActivityRepository activityRepository,
        IEmailAccessTokenProvider accessTokenProvider,
        IEmailProviderClientFactory clientFactory,
        IOptions<GmailOAuthSettings> settings,
        ILogger<CrmEmailSendService> logger)
    {
        _connectionRepository = connectionRepository;
        _sendOperationRepository = sendOperationRepository;
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _activityRepository = activityRepository;
        _accessTokenProvider = accessTokenProvider;
        _clientFactory = clientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CrmEmailSendResult> SendAsync(Guid clientOperationId, SendCrmEmailRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        if (request.ToAddresses is null || request.ToAddresses.Count == 0)
            return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.Validation, "At least one recipient is required.");
        if (string.IsNullOrWhiteSpace(request.BodyHtml))
            return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.Validation, "The message body is required.");

        var connection = await _connectionRepository.GetAsync(cancellationToken);
        if (connection is null || connection.Status != EmailConnectionStatus.Connected)
            return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.NotConnected, "No connected email account is available to send from.");

        CrmEmailThread? replyThread = null;
        if (request.ThreadId.HasValue)
        {
            replyThread = await _threadRepository.GetByIdAsync(request.ThreadId.Value, cancellationToken);
            if (replyThread is null || replyThread.ConnectionId != connection.Id)
                return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.Validation, "The conversation being replied to was not found.");
        }

        var operation = await _sendOperationRepository.GetByClientOperationIdAsync(clientOperationId, cancellationToken);
        if (operation is null)
        {
            var messageIdHeader = $"<{clientOperationId:N}@{_settings.OutgoingMessageIdDomain}>";
            operation = new EmailSendOperation(
                clientOperationId, connection.Id, messageIdHeader,
                request.ToAddresses, request.CcAddresses, request.BccAddresses,
                request.Subject, request.BodyHtml, request.TemplateId,
                request.CompanyId, request.ContactId, request.DealId, userId);
            await _sendOperationRepository.AddAsync(operation, cancellationToken);
            await _sendOperationRepository.SaveChangesAsync(cancellationToken);
        }

        if (operation.Status == EmailSendStatus.Accepted)
        {
            var existingMessage = await _messageRepository.GetBySendOperationIdAsync(operation.Id, cancellationToken);
            return existingMessage is null
                ? CrmEmailSendResult.Unconfirmed("The send was previously confirmed but its message could not be located.")
                : CrmEmailSendResult.Accepted(existingMessage.Id);
        }

        var client = _clientFactory.GetClient(connection.Provider);
        string accessToken;
        try
        {
            accessToken = await _accessTokenProvider.GetFreshAccessTokenAsync(connection, cancellationToken);
        }
        catch (EmailReauthRequiredException ex)
        {
            connection.MarkReauthRequired();
            await _connectionRepository.SaveChangesAsync(cancellationToken);
            return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.NotConnected, ex.Message);
        }

        // A prior attempt under this same clientOperationId didn't reach a
        // confirmed state (Submitting, left mid-flight by a crash, or
        // Failed, which may have actually gone out despite the error) — ask
        // the provider directly before ever sending again.
        if (operation.Status != EmailSendStatus.Pending)
        {
            var reconciled = await client.FindMessageByMessageIdHeaderAsync(accessToken, operation.GeneratedMessageIdHeader, cancellationToken);
            if (reconciled is not null)
            {
                operation.MarkAccepted(reconciled.ProviderMessageId);
                await _sendOperationRepository.SaveChangesAsync(cancellationToken);
                var message = await PersistSentMessageAsync(connection, operation, reconciled, cancellationToken);
                return CrmEmailSendResult.Accepted(message.Id);
            }
        }

        operation.MarkSubmitting();
        await _sendOperationRepository.SaveChangesAsync(cancellationToken);

        var outgoing = new OutgoingEmailMessage
        {
            FromAddress = connection.EmailAddress,
            ToAddresses = request.ToAddresses,
            CcAddresses = request.CcAddresses,
            BccAddresses = request.BccAddresses,
            Subject = request.Subject,
            BodyHtml = request.BodyHtml,
            MessageIdHeader = operation.GeneratedMessageIdHeader,
            ProviderThreadId = replyThread?.ProviderThreadId
        };

        var sendResult = await client.SendMessageAsync(accessToken, outgoing, cancellationToken);

        if (sendResult.Succeeded && sendResult.ProviderMessageId is not null)
        {
            operation.MarkAccepted(sendResult.ProviderMessageId);
            await _sendOperationRepository.SaveChangesAsync(cancellationToken);
            var detail = await client.GetMessageAsync(accessToken, sendResult.ProviderMessageId, cancellationToken);
            var message = await PersistSentMessageAsync(connection, operation, detail, cancellationToken);
            return CrmEmailSendResult.Accepted(message.Id);
        }

        if (sendResult.IsAmbiguous)
        {
            _logger.LogWarning("Send for operation {ClientOperationId} returned an ambiguous result — leaving it Submitting for reconciliation on retry.", clientOperationId);
            await _sendOperationRepository.SaveChangesAsync(cancellationToken);
            return CrmEmailSendResult.Unconfirmed(sendResult.ErrorMessage ?? "The send result is unconfirmed. Retry to check its status before sending again.");
        }

        operation.MarkFailed(sendResult.ErrorMessage ?? "The email provider rejected the message.");
        await _sendOperationRepository.SaveChangesAsync(cancellationToken);
        return CrmEmailSendResult.Failure(EmailSendStatus.Failed, CrmEmailSendError.ProviderError, operation.ErrorMessage!);
    }

    /// <summary>
    /// Mirrors EmailSyncProcessor.SyncOneMessageAsync's upsert shape, but
    /// associates the thread with whatever the composer explicitly
    /// selected (a deliberate user choice) rather than inferring it — and
    /// always applies it, unlike sync's new-thread-only auto-association.
    /// </summary>
    private async Task<CrmEmailMessage> PersistSentMessageAsync(EmailProviderConnection connection, EmailSendOperation operation, ProviderMessageDetail detail, CancellationToken cancellationToken)
    {
        var existingMessage = await _messageRepository.GetByConnectionAndProviderMessageIdAsync(connection.Id, detail.ProviderMessageId, cancellationToken);
        if (existingMessage is not null)
            return existingMessage;

        var thread = await _threadRepository.GetByProviderThreadIdAsync(connection.Id, detail.ProviderThreadId, cancellationToken);
        if (thread is null)
        {
            thread = new CrmEmailThread(connection.Id, detail.ProviderThreadId, detail.Subject, detail.OccurredAtUtc);
            await _threadRepository.AddAsync(thread, cancellationToken);
            await _threadRepository.SaveChangesAsync(cancellationToken);
        }

        var message = new CrmEmailMessage(
            thread.Id, connection.Id, detail.ProviderMessageId, EmailDirection.Outbound,
            detail.FromAddress, detail.FromName, detail.ToAddresses, detail.CcAddresses, detail.BccAddresses,
            detail.Subject, detail.BodyHtml, detail.BodyPlainText, detail.OccurredAtUtc,
            isRead: true, operation.Id);
        await _messageRepository.AddAsync(message, cancellationToken);

        foreach (var attachment in detail.Attachments)
        {
            await _messageRepository.AddAttachmentAsync(
                new CrmEmailAttachment(message.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.ProviderAttachmentId),
                cancellationToken);
        }

        thread.RegisterMessage(detail.OccurredAtUtc, detail.Subject, isUnread: false);

        if (operation.CompanyId.HasValue && await _threadRepository.GetCompanyLinkAsync(thread.Id, operation.CompanyId.Value, cancellationToken) is null)
            await _threadRepository.AddCompanyLinkAsync(new CrmEmailThreadCompany(thread.Id, operation.CompanyId.Value), cancellationToken);
        if (operation.ContactId.HasValue && await _threadRepository.GetContactLinkAsync(thread.Id, operation.ContactId.Value, cancellationToken) is null)
            await _threadRepository.AddContactLinkAsync(new CrmEmailThreadContact(thread.Id, operation.ContactId.Value), cancellationToken);
        if (operation.DealId.HasValue && await _threadRepository.GetDealLinkAsync(thread.Id, operation.DealId.Value, cancellationToken) is null)
            await _threadRepository.AddDealLinkAsync(new CrmEmailThreadDeal(thread.Id, operation.DealId.Value), cancellationToken);

        await _messageRepository.SaveChangesAsync(cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        // Mirrors the sync processor's own dedup guard: a message only ever
        // gets one CrmActivity row, created by whichever path (send or
        // sync) sees it first — CrmActivity.EmailMessageId is unique.
        var activity = new CrmActivity(
            CrmActivityType.EmailSent, detail.Subject, detail.OccurredAtUtc, operation.CreatedByUserId,
            companyId: operation.CompanyId, contactId: operation.ContactId, dealId: operation.DealId,
            description: null, outcome: null, emailMessageId: message.Id);
        await _activityRepository.AddAsync(activity, cancellationToken);
        await _activityRepository.SaveChangesAsync(cancellationToken);

        return message;
    }
}
