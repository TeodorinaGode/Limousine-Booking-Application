using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
