using LimousineBooking.Application.Account;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Moq;
using Xunit;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Tests.Account;

public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private AccountService CreateService() => new(_userRepository.Object);

    private static DomainUser MakeUser() =>
        new("jane.doe@example.com", "hash", "Jane", "Doe", UserRole.Administrator);

    [Fact]
    public async Task GetPreferencesAsync_ReturnsTheUsersSavedLanguage()
    {
        var user = MakeUser();
        user.SetLanguage("de");
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().GetPreferencesAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("de", result!.LanguageCode);
    }

    [Fact]
    public async Task GetPreferencesAsync_UnknownUser_ReturnsNull()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        var result = await CreateService().GetPreferencesAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_SavesNormalizedLanguageAndPersists()
    {
        var user = MakeUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService().UpdatePreferencesAsync(user.Id, new UpdateAccountPreferencesRequest { LanguageCode = "FR" });

        Assert.NotNull(result);
        Assert.Equal("fr", result!.LanguageCode);
        Assert.Equal("fr", user.LanguageCode);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UnsupportedCode_FallsBackToEnglishRatherThanFailing()
    {
        var user = MakeUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService().UpdatePreferencesAsync(user.Id, new UpdateAccountPreferencesRequest { LanguageCode = "es" });

        Assert.Equal("en", result!.LanguageCode);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_NullClearsThePreference()
    {
        var user = MakeUser();
        user.SetLanguage("de");
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService().UpdatePreferencesAsync(user.Id, new UpdateAccountPreferencesRequest { LanguageCode = null });

        Assert.Null(result!.LanguageCode);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UnknownUser_ReturnsNullWithoutSaving()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        var result = await CreateService().UpdatePreferencesAsync(Guid.NewGuid(), new UpdateAccountPreferencesRequest { LanguageCode = "de" });

        Assert.Null(result);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
