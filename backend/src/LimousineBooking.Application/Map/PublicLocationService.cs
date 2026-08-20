using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Map;

/// <inheritdoc cref="IPublicLocationService" />
public class PublicLocationService : IPublicLocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly MapSettings _settings;

    public PublicLocationService(ILocationRepository locationRepository, IOptions<MapSettings> settings)
    {
        _locationRepository = locationRepository;
        _settings = settings.Value;
    }

    public async Task<PublicLocationsResponse> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var response = new PublicLocationsResponse
        {
            Enabled = _settings.Enabled,
            Provider = _settings.Provider,
            DefaultLatitude = _settings.DefaultLatitude,
            DefaultLongitude = _settings.DefaultLongitude,
            DefaultZoom = _settings.DefaultZoom
        };

        if (!_settings.Enabled)
            return response;

        var locations = await _locationRepository.GetActiveAsync(cancellationToken);
        response.Locations = locations.Select(l => new PublicLocationResponse
        {
            Id = l.Id,
            Name = l.Name,
            CountryCode = l.CountryCode,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Type = l.Type.ToString(),
            Description = l.Description
        }).ToList();

        return response;
    }
}
