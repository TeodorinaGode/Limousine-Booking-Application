using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IEmailSendOperationRepository
{
    Task<EmailSendOperation?> GetByClientOperationIdAsync(Guid clientOperationId, CancellationToken cancellationToken = default);

    /// <summary>Correlates a message seen during sync with one we sent ourselves — the dedup guard that stops a sent-then-synced-back message from producing a second timeline entry.</summary>
    Task<EmailSendOperation?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default);

    Task AddAsync(EmailSendOperation operation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
