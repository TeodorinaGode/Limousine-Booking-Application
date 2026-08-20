using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Vehicles;

/// <inheritdoc cref="IPublicVehicleService" />
public class PublicVehicleService : IPublicVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public PublicVehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<IReadOnlyList<PublicVehicleResponse>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetActiveAsync(cancellationToken);

        return vehicles.Select(v => new PublicVehicleResponse
        {
            Id = v.Id,
            Make = v.Make,
            Model = v.Model,
            VehicleType = v.VehicleType,
            PassengerCapacity = v.PassengerCapacity
        }).ToList();
    }
}
