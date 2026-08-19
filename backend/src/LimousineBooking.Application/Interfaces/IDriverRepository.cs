using LimousineBooking.Application.Drivers;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IDriverRepository
{
    /// <summary>Used by authentication to resolve the driverId JWT claim — no includes.</summary>
    Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Includes User and CurrentVehicle, for admin driver-management responses.</summary>
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Driver> Items, int TotalCount)> SearchAsync(DriverSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if this vehicle is currently assigned as another driver's CurrentVehicle,
    /// excluding <paramref name="excludeDriverId"/> if given. Enforces "one vehicle,
    /// zero or one current driver."
    /// </summary>
    Task<bool> IsVehicleAssignedToAnotherDriverAsync(Guid vehicleId, Guid? excludeDriverId, CancellationToken cancellationToken = default);

    Task AddAsync(Driver driver, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
