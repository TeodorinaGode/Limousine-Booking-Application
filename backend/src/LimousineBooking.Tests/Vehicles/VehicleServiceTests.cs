using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Vehicles;
using Moq;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Vehicles;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();

    private VehicleService CreateService() => new(_vehicleRepository.Object);

    private static CreateVehicleRequest ValidCreateRequest() => new()
    {
        RegistrationNumber = "BS 123456",
        Make = "Mercedes-Benz",
        Model = "V-Class",
        VehicleType = "Van",
        PassengerCapacity = 7,
        Notes = "Executive vehicle"
    };

    // ---- Create ----

    [Fact]
    public async Task Create_WithValidData_Succeeds()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync("BS 123456", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("BS 123456", result.Vehicle!.RegistrationNumber);
        Assert.True(result.Vehicle!.IsActive); // active by default
        _vehicleRepository.Verify(r => r.AddAsync(It.IsAny<DomainVehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        _vehicleRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithMissingRegistration_IsRejected()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.RegistrationNumber = "   ";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithMissingMake_IsRejected()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Make = "";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithMissingModel_IsRejected()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Model = "";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithMissingVehicleType_IsRejected()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.VehicleType = "  ";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Validation, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WithNonPositiveCapacity_IsRejected(int capacity)
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.PassengerCapacity = capacity;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_DuplicateRegistration_IsRejected()
    {
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync("BS 123456", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.Duplicate, result.Error);
        _vehicleRepository.Verify(r => r.AddAsync(It.IsAny<DomainVehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("bs 123456")]
    [InlineData(" BS 123456")]
    [InlineData("BS   123456")]
    [InlineData("  bs 123456  ")]
    public async Task Create_NormalizesRegistrationNumber_BeforeDuplicateCheckAndStorage(string rawRegistration)
    {
        var request = ValidCreateRequest();
        request.RegistrationNumber = rawRegistration;

        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync("BS 123456", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("BS 123456", result.Vehicle!.RegistrationNumber);
        _vehicleRepository.Verify(r => r.HasDuplicateRegistrationAsync("BS 123456", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Update ----

    private static DomainVehicle ExistingVehicle() => new("BS 123456", "Mercedes-Benz", "V-Class", "Van", 7);

    [Fact]
    public async Task Update_ExistingVehicle_Succeeds()
    {
        var vehicle = ExistingVehicle();
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _vehicleRepository.Setup(r => r.HasDuplicateRegistrationAsync("BS 789012", vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new UpdateVehicleRequest
        {
            RegistrationNumber = "BS 789012",
            Make = "Mercedes-Benz",
            Model = "S-Class",
            VehicleType = "Sedan",
            PassengerCapacity = 3,
            IsActive = true
        };

        var result = await CreateService().UpdateAsync(vehicle.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal("BS 789012", result.Vehicle!.RegistrationNumber);
        Assert.Equal("S-Class", result.Vehicle!.Model);
        _vehicleRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistingVehicle_ReturnsNotFound()
    {
        _vehicleRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainVehicle?)null);

        var result = await CreateService().UpdateAsync(Guid.NewGuid(), new UpdateVehicleRequest
        {
            RegistrationNumber = "BS 789012",
            Make = "Mercedes-Benz",
            Model = "S-Class",
            VehicleType = "Sedan",
            PassengerCapacity = 3,
            IsActive = true
        });

        Assert.False(result.Succeeded);
        Assert.Equal(VehicleError.NotFound, result.Error);
    }

    // ---- Activation ----

    [Fact]
    public async Task SetActive_True_ActivatesVehicle()
    {
        var vehicle = ExistingVehicle();
        vehicle.Deactivate();
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var result = await CreateService().SetActiveAsync(vehicle.Id, true);

        Assert.True(result.Succeeded);
        Assert.True(result.Vehicle!.IsActive);
    }

    [Fact]
    public async Task SetActive_False_DeactivatesVehicle_WithoutDeletingIt()
    {
        var vehicle = ExistingVehicle();
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var result = await CreateService().SetActiveAsync(vehicle.Id, false);

        Assert.True(result.Succeeded);
        Assert.False(result.Vehicle!.IsActive);
        // The vehicle itself is still returned/retrievable — deactivation is not deletion.
        Assert.Equal(vehicle.Id, result.Vehicle!.Id);
        _vehicleRepository.Verify(r => r.AddAsync(It.IsAny<DomainVehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
