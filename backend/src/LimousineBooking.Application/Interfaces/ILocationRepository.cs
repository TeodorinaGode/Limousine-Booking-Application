using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetActiveAsync(CancellationToken cancellationToken = default);
}
