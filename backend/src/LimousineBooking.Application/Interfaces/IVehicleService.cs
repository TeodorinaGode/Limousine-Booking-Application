using LimousineBooking.Application.Common;
using LimousineBooking.Application.Vehicles;

namespace LimousineBooking.Application.Interfaces;

public interface IVehicleService
{
    Task<PagedResult<VehicleResponse>> SearchAsync(VehicleSearchQuery query, CancellationToken cancellationToken = default);

    Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VehicleOperationResult> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleOperationResult> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
