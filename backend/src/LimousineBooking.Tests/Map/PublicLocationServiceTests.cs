using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Map;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainLocation = LimousineBooking.Domain.Entities.Location;

namespace LimousineBooking.Tests.Map;

public class PublicLocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepository = new();

    private PublicLocationService CreateService(MapSettings? settings = null) =>
        new(_locationRepository.Object, Options.Create(settings ?? new MapSettings()));

    [Fact]
    public async Task GetLocationsAsync_MapsActiveLocationsToPublicFields()
    {
        var location = new DomainLocation("Basel", "CH", 47.5596, 7.5886, LocationType.City, "Major Swiss city", 1);
        _locationRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { location });

        var result = await CreateService().GetLocationsAsync();

        var pin = Assert.Single(result.Locations);
        Assert.Equal(location.Id, pin.Id);
        Assert.Equal("Basel", pin.Name);
        Assert.Equal("CH", pin.CountryCode);
        Assert.Equal(47.5596, pin.Latitude);
        Assert.Equal(7.5886, pin.Longitude);
        Assert.Equal("City", pin.Type);
        Assert.Equal("Major Swiss city", pin.Description);
    }

    [Fact]
    public async Task GetLocationsAsync_IncludesMapRenderingSettings()
    {
        var settings = new MapSettings { Provider = "leaflet", DefaultLatitude = 47.0, DefaultLongitude = 8.5, DefaultZoom = 6, Enabled = true };
        _locationRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DomainLocation>());

        var result = await CreateService(settings).GetLocationsAsync();

        Assert.True(result.Enabled);
        Assert.Equal("leaflet", result.Provider);
        Assert.Equal(47.0, result.DefaultLatitude);
        Assert.Equal(8.5, result.DefaultLongitude);
        Assert.Equal(6, result.DefaultZoom);
    }

    [Fact]
    public async Task GetLocationsAsync_WhenDisabled_ReturnsNoLocationsAndNeverQueriesTheRepository()
    {
        var settings = new MapSettings { Enabled = false };

        var result = await CreateService(settings).GetLocationsAsync();

        Assert.False(result.Enabled);
        Assert.Empty(result.Locations);
        _locationRepository.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLocationsAsync_NoActiveLocations_ReturnsEmptyList()
    {
        _locationRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DomainLocation>());

        var result = await CreateService().GetLocationsAsync();

        Assert.Empty(result.Locations);
    }
}
