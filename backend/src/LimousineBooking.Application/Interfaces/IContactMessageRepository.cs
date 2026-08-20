using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IContactMessageRepository
{
    Task AddAsync(ContactMessage message, CancellationToken cancellationToken = default);

    /// <summary>The oldest <paramref name="batchSize"/> still-Pending messages — this outbox is small/low-volume, so no retry/backoff scheduling is needed, unlike the booking Notification outbox.</summary>
    Task<IReadOnlyList<ContactMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
