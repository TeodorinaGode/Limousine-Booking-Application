using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IRideStatusHistoryRepository
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<RideStatusHistory>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(RideStatusHistory history, CancellationToken cancellationToken = default);
}
