using LimousineBooking.Application.Authentication;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Moq;

namespace LimousineBooking.Tests.Authentication;

public class LoginHandlerTests
{
    private const string RawPassword = "Test#Passw0rd!";
    private const string StoredHash = "hashed:Test#Passw0rd!";

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();

    private LoginHandler CreateHandler() =>
        new(_userRepository.Object, _driverRepository.Object, _passwordService.Object, _jwtTokenService.Object);

    private static User CreateUser(UserRole role, bool isActive = true) =>
        new("user@example.com", StoredHash, "Test", "User", role);

    [Fact]
    public async Task Login_WithValidAdministratorCredentials_Succeeds()
    {
        var admin = CreateUser(UserRole.Administrator);
        _userRepository.Setup(r => r.GetByEmailAsync(admin.Email, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _passwordService.Setup(p => p.Verify(StoredHash, RawPassword)).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(admin, null))
            .Returns(new JwtToken("admin-token", DateTime.UtcNow.AddHours(1)));

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = admin.Email, Password = RawPassword });

        Assert.True(outcome.Succeeded);
        Assert.Equal("Administrator", outcome.Response!.User.Role);
    }

    [Fact]
    public async Task Login_WithValidDriverCredentials_Succeeds()
    {
        var driverUser = CreateUser(UserRole.Driver);
        var driver = new Driver(driverUser.Id, "+41791234567");

        _userRepository.Setup(r => r.GetByEmailAsync(driverUser.Email, It.IsAny<CancellationToken>())).ReturnsAsync(driverUser);
        _driverRepository.Setup(r => r.GetByUserIdAsync(driverUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _passwordService.Setup(p => p.Verify(StoredHash, RawPassword)).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(driverUser, driver))
            .Returns(new JwtToken("driver-token", DateTime.UtcNow.AddHours(1)));

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = driverUser.Email, Password = RawPassword });

        Assert.True(outcome.Succeeded);
        Assert.Equal("Driver", outcome.Response!.User.Role);
        _driverRepository.Verify(r => r.GetByUserIdAsync(driverUser.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_JwtIsReturned_OnSuccess()
    {
        var admin = CreateUser(UserRole.Administrator);
        _userRepository.Setup(r => r.GetByEmailAsync(admin.Email, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _passwordService.Setup(p => p.Verify(StoredHash, RawPassword)).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(admin, null))
            .Returns(new JwtToken("a-valid-jwt", DateTime.UtcNow.AddHours(1)));

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = admin.Email, Password = RawPassword });

        Assert.Equal("a-valid-jwt", outcome.Response!.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Fails()
    {
        var admin = CreateUser(UserRole.Administrator);
        _userRepository.Setup(r => r.GetByEmailAsync(admin.Email, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _passwordService.Setup(p => p.Verify(StoredHash, "wrong-password")).Returns(false);

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = admin.Email, Password = "wrong-password" });

        Assert.False(outcome.Succeeded);
        Assert.Null(outcome.Response);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Fails()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = RawPassword });

        Assert.False(outcome.Succeeded);
        Assert.Null(outcome.Response);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Fails()
    {
        var admin = CreateUser(UserRole.Administrator);
        admin.Deactivate();
        _userRepository.Setup(r => r.GetByEmailAsync(admin.Email, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var outcome = await CreateHandler().LoginAsync(new LoginRequest { Email = admin.Email, Password = RawPassword });

        Assert.False(outcome.Succeeded);
        // Password should never even be checked for an inactive account.
        _passwordService.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ProduceIdenticalFailureShape()
    {
        var admin = CreateUser(UserRole.Administrator);
        _userRepository.Setup(r => r.GetByEmailAsync(admin.Email, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _passwordService.Setup(p => p.Verify(StoredHash, "wrong-password")).Returns(false);
        _userRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var wrongPasswordOutcome = await CreateHandler().LoginAsync(new LoginRequest { Email = admin.Email, Password = "wrong-password" });
        var unknownEmailOutcome = await CreateHandler().LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = RawPassword });

        Assert.Equal(wrongPasswordOutcome.Succeeded, unknownEmailOutcome.Succeeded);
        Assert.Equal(wrongPasswordOutcome.Response, unknownEmailOutcome.Response);
    }
}
