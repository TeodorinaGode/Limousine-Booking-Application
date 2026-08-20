using LimousineBooking.Application.Vehicles;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> SearchAsync(VehicleSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>All active vehicles, for the public Fleet page. No pagination — this is a small, public reference list.</summary>
    Task<IReadOnlyList<Vehicle>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True if any vehicle (active or inactive) already has this registration
    /// number (normalized, case-insensitive), excluding <paramref name="excludeVehicleId"/> if given.
    /// A registration number identifies one physical vehicle permanently, so
    /// this check is global — unlike Route's active-only duplicate rule.
    /// </summary>
    Task<bool> HasDuplicateRegistrationAsync(string registrationNumber, Guid? excludeVehicleId, CancellationToken cancellationToken = default);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
