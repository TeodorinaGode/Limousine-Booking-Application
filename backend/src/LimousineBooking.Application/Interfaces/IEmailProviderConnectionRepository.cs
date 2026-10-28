using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IEmailProviderConnectionRepository
{
    /// <summary>The one active connection this CRM manages, if any — most recently created, regardless of status (Disconnected rows are kept for audit, never deleted).</summary>
    Task<EmailProviderConnection?> GetAsync(CancellationToken cancellationToken = default);

    Task<EmailProviderConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(EmailProviderConnection connection, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
