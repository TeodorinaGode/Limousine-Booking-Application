using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IEmailSyncCheckpointRepository
{
    Task<EmailSyncCheckpoint?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default);

    Task AddAsync(EmailSyncCheckpoint checkpoint, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
