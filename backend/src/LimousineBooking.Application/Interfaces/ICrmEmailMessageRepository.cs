using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmEmailMessageRepository
{
    Task<bool> ExistsAsync(Guid connectionId, string providerMessageId, CancellationToken cancellationToken = default);

    Task<CrmEmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmEmailMessage?> GetByConnectionAndProviderMessageIdAsync(Guid connectionId, string providerMessageId, CancellationToken cancellationToken = default);

    /// <summary>Looks up the CrmEmailMessage a confirmed send operation produced — used to return the same result on an idempotent replay without calling the provider again.</summary>
    Task<CrmEmailMessage?> GetBySendOperationIdAsync(Guid sendOperationId, CancellationToken cancellationToken = default);

    /// <summary>Batch-resolves ThreadId for a page of activity rows in one query, rather than N+1 — used only to deep-link an email-sourced CrmActivity to its conversation.</summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetThreadIdsByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken = default);

    Task AddAsync(CrmEmailMessage message, CancellationToken cancellationToken = default);

    Task AddAttachmentAsync(CrmEmailAttachment attachment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
