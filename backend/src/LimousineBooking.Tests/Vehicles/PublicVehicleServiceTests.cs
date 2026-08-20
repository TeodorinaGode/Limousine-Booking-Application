using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Vehicles;
using Moq;
using Xunit;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Vehicles;

public class PublicVehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();

    private PublicVehicleService CreateService() => new(_vehicleRepository.Object);

    [Fact]
    public async Task GetActiveVehiclesAsync_MapsOnlyPublicSafeFields()
    {
        var vehicle = new DomainVehicle("BS 123456", "Mercedes-Benz", "S-Class", "Sedan", 3, "Internal maintenance note");
        _vehicleRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { vehicle });

        var result = await CreateService().GetActiveVehiclesAsync();

        var response = Assert.Single(result);
        Assert.Equal(vehicle.Id, response.Id);
        Assert.Equal("Mercedes-Benz", response.Make);
        Assert.Equal("S-Class", response.Model);
        Assert.Equal("Sedan", response.VehicleType);
        Assert.Equal(3, response.PassengerCapacity);
    }

    [Fact]
    public async Task GetActiveVehiclesAsync_NeverExposesRegistrationNumberOrNotes()
    {
        var vehicle = new DomainVehicle("BS 999999", "Mercedes-Benz", "V-Class", "Van", 6, "Do not send on long trips — brakes due for service");
        _vehicleRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { vehicle });

        var response = (await CreateService().GetActiveVehiclesAsync()).Single();
        var serialized = System.Text.Json.JsonSerializer.Serialize(response);

        Assert.DoesNotContain("BS 999999", serialized);
        Assert.DoesNotContain("brakes", serialized);
    }

    [Fact]
    public async Task GetActiveVehiclesAsync_NoActiveVehicles_ReturnsEmptyList()
    {
        _vehicleRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DomainVehicle>());

        var result = await CreateService().GetActiveVehiclesAsync();

        Assert.Empty(result);
    }
}
