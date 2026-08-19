using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IAssignmentHistoryRepository
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<AssignmentHistory>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(AssignmentHistory history, CancellationToken cancellationToken = default);
}
