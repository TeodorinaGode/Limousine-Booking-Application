using LimousineBooking.Application.Map;

namespace LimousineBooking.Application.Interfaces;

public interface IPublicLocationService
{
    Task<PublicLocationsResponse> GetLocationsAsync(CancellationToken cancellationToken = default);
}
