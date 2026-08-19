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
    /// Drivers who pass every automatic-assignment eligibility check that can be
    /// expressed as a database filter: active, linked User active, currently
    /// available, has an active current vehicle with at least
    /// <paramref name="minPassengerCapacity"/> seats. Includes User and
    /// CurrentVehicle. Scheduled-availability and booking-conflict checks happen
    /// afterward — those need per-candidate queries this filter can't express.
    /// </summary>
    Task<IReadOnlyList<Driver>> GetAssignmentCandidatesAsync(int minPassengerCapacity, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if this vehicle is currently assigned as another driver's CurrentVehicle,
    /// excluding <paramref name="excludeDriverId"/> if given. Enforces "one vehicle,
    /// zero or one current driver."
    /// </summary>
    Task<bool> IsVehicleAssignedToAnotherDriverAsync(Guid vehicleId, Guid? excludeDriverId, CancellationToken cancellationToken = default);

    Task AddAsync(Driver driver, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
