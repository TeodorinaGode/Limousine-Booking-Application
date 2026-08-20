using LimousineBooking.Application.Vehicles;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Anonymous, read-only vehicle listing for the public Fleet page (Prompt 17).</summary>
public interface IPublicVehicleService
{
    Task<IReadOnlyList<PublicVehicleResponse>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default);
}
