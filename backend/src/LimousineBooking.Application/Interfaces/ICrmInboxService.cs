using LimousineBooking.Application.Common;
using LimousineBooking.Application.Email;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Read/organize side of the CRM inbox — searching synced threads, viewing a
/// conversation, and editing its company/contact/deal associations. Sending
/// lives in a separate service (Phase 5); this one never talks to the
/// provider except to stream an attachment's bytes on demand.
/// </summary>
public interface ICrmInboxService
{
    Task<PagedResult<CrmEmailThreadListItemResponse>> SearchThreadsAsync(CrmEmailThreadSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmEmailThreadDetailResponse?> GetThreadDetailAsync(Guid threadId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> SetReadAsync(Guid threadId, bool isRead, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> LinkCompanyAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> UnlinkCompanyAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> LinkContactAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> UnlinkContactAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> LinkDealAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default);

    Task<CrmInboxOperationResult> UnlinkDealAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default);

    /// <summary>"Remove from CRM" — hides the thread from the Inbox list only; the real Gmail mailbox is never touched (see CrmEmailThread.Remove). Returns false when the thread doesn't exist.</summary>
    Task<bool> RemoveThreadAsync(Guid threadId, CancellationToken cancellationToken = default);

    /// <summary>Streams the attachment's bytes live from the provider — never from local storage. Null when the message/attachment doesn't exist or the attachment doesn't belong to that message.</summary>
    Task<CrmEmailAttachmentDownload?> GetAttachmentAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken = default);
}
