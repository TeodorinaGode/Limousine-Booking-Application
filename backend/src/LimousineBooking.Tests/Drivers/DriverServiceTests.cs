using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Moq;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainUser = LimousineBooking.Domain.Entities.User;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Tests.Drivers;

public class DriverServiceTests
{
    private const string HashedPassword = "hashed:Test#Passw0rd!";

    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IVehicleRepository> _vehicleRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();

    public DriverServiceTests()
    {
        _passwordService.Setup(p => p.Hash(It.IsAny<string>())).Returns(HashedPassword);
    }

    private DriverService CreateService() =>
        new(_driverRepository.Object, _userRepository.Object, _vehicleRepository.Object, _passwordService.Object);

    private static CreateDriverRequest ValidCreateRequest() => new()
    {
        FirstName = "John",
        LastName = "Smith",
        Email = "John.Smith@example.com",
        Phone = "+41 79 123 4567",
        Password = "Test#Passw0rd!",
        VehicleId = null
    };

    private static DomainVehicle ActiveVehicle() => new("BS 123456", "Mercedes-Benz", "V-Class", "Van", 7);

    // ---- Create ----

    [Fact]
    public async Task Create_WithValidData_Succeeds()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("john.smith@example.com", result.Driver!.Email); // normalized to lowercase
    }

    [Fact]
    public async Task Create_CreatesUserAndDriverTogether()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateService().CreateAsync(ValidCreateRequest());

        _userRepository.Verify(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _driverRepository.Verify(r => r.AddAsync(It.IsAny<DomainDriver>(), It.IsAny<CancellationToken>()), Times.Once);
        _driverRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_AssignsDriverRole()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        DomainUser? capturedUser = null;
        _userRepository.Setup(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()))
            .Callback<DomainUser, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(ValidCreateRequest());

        Assert.Equal(UserRole.Driver, capturedUser!.Role);
    }

    [Fact]
    public async Task Create_WithMissingFirstName_IsRejected()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.FirstName = "  ";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithMissingLastName_IsRejected()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.LastName = "";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithInvalidEmailFormat_IsRejected()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Email = "not-an-email";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_IsRejected()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync("john.smith@example.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Duplicate, result.Error);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_EmailComparisonIsCaseInsensitive()
    {
        // The service normalizes to lowercase before calling HasDuplicateEmailAsync,
        // so "John.Smith@example.com" and "john.smith@example.com" hit the same check.
        _userRepository.Setup(r => r.HasDuplicateEmailAsync("john.smith@example.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.True(result.Succeeded);
        _userRepository.Verify(r => r.HasDuplicateEmailAsync("john.smith@example.com", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_HashesThePassword()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        DomainUser? capturedUser = null;
        _userRepository.Setup(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()))
            .Callback<DomainUser, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        await CreateService().CreateAsync(ValidCreateRequest());

        _passwordService.Verify(p => p.Hash("Test#Passw0rd!"), Times.Once);
        Assert.Equal(HashedPassword, capturedUser!.PasswordHash);
        Assert.NotEqual("Test#Passw0rd!", capturedUser.PasswordHash);
    }

    // ---- Vehicle assignment ----

    [Fact]
    public async Task Create_WithoutVehicle_Succeeds()
    {
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.True(result.Succeeded);
        Assert.Null(result.Driver!.Vehicle);
    }

    [Fact]
    public async Task Create_WithActiveVehicle_AssignsIt()
    {
        var vehicle = ActiveVehicle();
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _driverRepository.Setup(r => r.IsVehicleAssignedToAnotherDriverAsync(vehicle.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = ValidCreateRequest();
        request.VehicleId = vehicle.Id;

        var result = await CreateService().CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(vehicle.Id, result.Driver!.Vehicle!.Id);
    }

    [Fact]
    public async Task Create_WithInactiveVehicle_IsRejected()
    {
        var vehicle = ActiveVehicle();
        vehicle.Deactivate();
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var request = ValidCreateRequest();
        request.VehicleId = vehicle.Id;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Validation, result.Error);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<DomainUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithVehicleAlreadyAssignedToAnotherDriver_IsRejected()
    {
        var vehicle = ActiveVehicle();
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _driverRepository.Setup(r => r.IsVehicleAssignedToAnotherDriverAsync(vehicle.Id, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = ValidCreateRequest();
        request.VehicleId = vehicle.Id;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Duplicate, result.Error);
    }

    [Fact]
    public async Task Update_RemovingVehicleId_UnassignsIt()
    {
        var user = new DomainUser("john.smith@example.com", HashedPassword, "John", "Smith", UserRole.Driver);
        var vehicle = ActiveVehicle();
        var driver = new DomainDriver(user.Id, "+41791234567");
        driver.AssignVehicle(vehicle.Id);
        SetNavigation(driver, user, vehicle);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new UpdateDriverRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@example.com",
            Phone = "+41791234567",
            IsActive = true,
            VehicleId = null
        };

        var result = await CreateService().UpdateAsync(driver.Id, request);

        Assert.True(result.Succeeded);
        Assert.Null(result.Driver!.Vehicle);
        Assert.Null(driver.CurrentVehicleId);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_ExistingDriver_Succeeds()
    {
        var user = new DomainUser("john.smith@example.com", HashedPassword, "John", "Smith", UserRole.Driver);
        var driver = new DomainDriver(user.Id, "+41791234567");
        SetNavigation(driver, user, null);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _userRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new UpdateDriverRequest
        {
            FirstName = "Jonathan",
            LastName = "Smith",
            Email = "john.smith@example.com",
            Phone = "+41791234567",
            IsActive = true,
            VehicleId = null
        };

        var result = await CreateService().UpdateAsync(driver.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal("Jonathan", result.Driver!.FirstName);
        _driverRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistingDriver_ReturnsNotFound()
    {
        _driverRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainDriver?)null);

        var result = await CreateService().UpdateAsync(Guid.NewGuid(), new UpdateDriverRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@example.com",
            Phone = "+41791234567",
            IsActive = true
        });

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_ToAnotherUsersEmail_IsRejected()
    {
        var user = new DomainUser("john.smith@example.com", HashedPassword, "John", "Smith", UserRole.Driver);
        var driver = new DomainDriver(user.Id, "+41791234567");
        SetNavigation(driver, user, null);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _userRepository.Setup(r => r.HasDuplicateEmailAsync("taken@example.com", user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdateDriverRequest
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "taken@example.com",
            Phone = "+41791234567",
            IsActive = true
        };

        var result = await CreateService().UpdateAsync(driver.Id, request);

        Assert.False(result.Succeeded);
        Assert.Equal(DriverError.Duplicate, result.Error);
    }

    // ---- Status ----

    [Fact]
    public async Task SetActive_False_DeactivatesDriverAndUser_WithoutDeletingEither()
    {
        var user = new DomainUser("john.smith@example.com", HashedPassword, "John", "Smith", UserRole.Driver);
        var driver = new DomainDriver(user.Id, "+41791234567");
        SetNavigation(driver, user, null);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var result = await CreateService().SetActiveAsync(driver.Id, false);

        Assert.True(result.Succeeded);
        Assert.False(result.Driver!.IsActive);
        Assert.Equal(driver.Id, result.Driver.Id); // still retrievable — not deleted
        Assert.False(user.IsActive);
        Assert.False(driver.IsActive);
    }

    [Fact]
    public async Task SetActive_True_ActivatesDriverAndUser()
    {
        var user = new DomainUser("john.smith@example.com", HashedPassword, "John", "Smith", UserRole.Driver);
        user.Deactivate();
        var driver = new DomainDriver(user.Id, "+41791234567");
        driver.Deactivate();
        SetNavigation(driver, user, null);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var result = await CreateService().SetActiveAsync(driver.Id, true);

        Assert.True(result.Succeeded);
        Assert.True(user.IsActive);
        Assert.True(driver.IsActive);
    }

    private static void SetNavigation(DomainDriver driver, DomainUser user, DomainVehicle? vehicle)
    {
        typeof(DomainDriver).GetProperty(nameof(DomainDriver.User))!.SetValue(driver, user);
        typeof(DomainDriver).GetProperty(nameof(DomainDriver.CurrentVehicle))!.SetValue(driver, vehicle);
    }
}
