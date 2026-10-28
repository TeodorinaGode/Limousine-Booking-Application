using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Email;

public class CrmInboxService : ICrmInboxService
{
    private readonly ICrmEmailThreadRepository _threadRepository;
    private readonly ICrmEmailMessageRepository _messageRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmDealRepository _dealRepository;
    private readonly IEmailProviderConnectionRepository _connectionRepository;
    private readonly IEmailAccessTokenProvider _accessTokenProvider;
    private readonly IEmailProviderClientFactory _clientFactory;
    private readonly IHtmlSanitizerService _htmlSanitizer;

    public CrmInboxService(
        ICrmEmailThreadRepository threadRepository,
        ICrmEmailMessageRepository messageRepository,
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        ICrmDealRepository dealRepository,
        IEmailProviderConnectionRepository connectionRepository,
        IEmailAccessTokenProvider accessTokenProvider,
        IEmailProviderClientFactory clientFactory,
        IHtmlSanitizerService htmlSanitizer)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _dealRepository = dealRepository;
        _connectionRepository = connectionRepository;
        _accessTokenProvider = accessTokenProvider;
        _clientFactory = clientFactory;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<PagedResult<CrmEmailThreadListItemResponse>> SearchThreadsAsync(CrmEmailThreadSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _threadRepository.SearchAsync(query, cancellationToken);
        return new PagedResult<CrmEmailThreadListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmEmailThreadDetailResponse?> GetThreadDetailAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetDetailByIdAsync(threadId, cancellationToken);
        return thread is null ? null : ToDetailResponse(thread);
    }

    public async Task<CrmInboxOperationResult> SetReadAsync(Guid threadId, bool isRead, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetDetailByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        if (isRead)
            thread.MarkRead();
        else
            thread.MarkUnread();

        await _threadRepository.SaveChangesAsync(cancellationToken);

        return CrmInboxOperationResult.Success(ToDetailResponse(thread));
    }

    public async Task<CrmInboxOperationResult> LinkCompanyAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "Company not found.");

        var existing = await _threadRepository.GetCompanyLinkAsync(threadId, companyId, cancellationToken);
        if (existing is not null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Duplicate, "This company is already linked to this conversation.");

        await _threadRepository.AddCompanyLinkAsync(new CrmEmailThreadCompany(threadId, companyId), cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmInboxOperationResult> UnlinkCompanyAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var link = await _threadRepository.GetCompanyLinkAsync(threadId, companyId, cancellationToken);
        if (link is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "This company is not linked to this conversation.");

        await _threadRepository.RemoveCompanyLinkAsync(link, cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmInboxOperationResult> LinkContactAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);
        if (contact is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "Contact not found.");

        var existing = await _threadRepository.GetContactLinkAsync(threadId, contactId, cancellationToken);
        if (existing is not null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Duplicate, "This contact is already linked to this conversation.");

        await _threadRepository.AddContactLinkAsync(new CrmEmailThreadContact(threadId, contactId), cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmInboxOperationResult> UnlinkContactAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var link = await _threadRepository.GetContactLinkAsync(threadId, contactId, cancellationToken);
        if (link is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "This contact is not linked to this conversation.");

        await _threadRepository.RemoveContactLinkAsync(link, cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmInboxOperationResult> LinkDealAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "Deal not found.");

        var existing = await _threadRepository.GetDealLinkAsync(threadId, dealId, cancellationToken);
        if (existing is not null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Duplicate, "This deal is already linked to this conversation.");

        await _threadRepository.AddDealLinkAsync(new CrmEmailThreadDeal(threadId, dealId), cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmInboxOperationResult> UnlinkDealAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, cancellationToken);
        if (thread is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.NotFound, "Thread not found.");

        var link = await _threadRepository.GetDealLinkAsync(threadId, dealId, cancellationToken);
        if (link is null)
            return CrmInboxOperationResult.Failure(CrmInboxError.Validation, "This deal is not linked to this conversation.");

        await _threadRepository.RemoveDealLinkAsync(link, cancellationToken);
        await _threadRepository.SaveChangesAsync(cancellationToken);

        return await ReloadDetailAsync(threadId, cancellationToken);
    }

    public async Task<CrmEmailAttachmentDownload?> GetAttachmentAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null)
            return null;

        var attachment = message.Attachments.SingleOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
            return null;

        var connection = await _connectionRepository.GetByIdAsync(message.ConnectionId, cancellationToken);
        if (connection is null)
            return null;

        var accessToken = await _accessTokenProvider.GetFreshAccessTokenAsync(connection, cancellationToken);
        var client = _clientFactory.GetClient(connection.Provider);
        var content = await client.GetAttachmentAsync(accessToken, message.ProviderMessageId, attachment.ProviderAttachmentId, cancellationToken);

        return new CrmEmailAttachmentDownload
        {
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            Content = content
        };
    }

    private async Task<CrmInboxOperationResult> ReloadDetailAsync(Guid threadId, CancellationToken cancellationToken)
    {
        var detail = await _threadRepository.GetDetailByIdAsync(threadId, cancellationToken);
        return CrmInboxOperationResult.Success(ToDetailResponse(detail!));
    }

    private CrmEmailThreadDetailResponse ToDetailResponse(CrmEmailThread thread) => new()
    {
        Id = thread.Id,
        Subject = thread.Subject,
        HasUnread = thread.HasUnread,
        Messages = thread.Messages
            .OrderBy(m => m.OccurredAt)
            .Select(ToMessageResponse)
            .ToList(),
        Companies = thread.ThreadCompanies
            .Where(tc => tc.Company is not null)
            .Select(tc => new CrmCompanySummary { Id = tc.Company!.Id, Name = tc.Company.Name, CompanyType = tc.Company.CompanyType.ToString() })
            .ToList(),
        Contacts = thread.ThreadContacts
            .Where(tc => tc.Contact is not null)
            .Select(tc => new CrmContactSummary { Id = tc.Contact!.Id, FirstName = tc.Contact.FirstName, LastName = tc.Contact.LastName, Email = tc.Contact.Email, JobTitle = tc.Contact.JobTitle })
            .ToList(),
        Deals = thread.ThreadDeals
            .Where(td => td.Deal is not null)
            .Select(td => new CrmDealSummary
            {
                Id = td.Deal!.Id,
                Name = td.Deal.Name,
                Value = td.Deal.Value,
                Currency = td.Deal.Currency,
                StageId = td.Deal.StageId,
                StageName = td.Deal.Stage?.Name ?? string.Empty,
                Probability = td.Deal.Probability,
                IsWon = td.Deal.Stage?.IsWon ?? false,
                IsLost = td.Deal.Stage?.IsLost ?? false
            })
            .ToList()
    };

    private CrmEmailMessageResponse ToMessageResponse(CrmEmailMessage message) => new()
    {
        Id = message.Id,
        Direction = message.Direction.ToString(),
        FromAddress = message.FromAddress,
        FromName = message.FromName,
        ToAddresses = EmailAddressListSerializer.Deserialize(message.ToAddressesJson),
        CcAddresses = EmailAddressListSerializer.Deserialize(message.CcAddressesJson),
        BccAddresses = EmailAddressListSerializer.Deserialize(message.BccAddressesJson),
        Subject = message.Subject,
        BodyHtml = message.BodyHtml is null ? null : _htmlSanitizer.Sanitize(message.BodyHtml),
        BodyPlainText = message.BodyPlainText,
        OccurredAt = message.OccurredAt,
        IsRead = message.IsRead,
        Attachments = message.Attachments
            .Select(a => new CrmEmailAttachmentResponse { Id = a.Id, FileName = a.FileName, ContentType = a.ContentType, SizeBytes = a.SizeBytes })
            .ToList()
    };
}
