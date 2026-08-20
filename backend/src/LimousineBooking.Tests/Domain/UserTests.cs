using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class UserTests
{
    private static User CreateUser() =>
        new("jane.doe@example.com", "hash", "Jane", "Doe", UserRole.Administrator);

    [Fact]
    public void NewUser_HasNoLanguagePreference()
    {
        var user = CreateUser();

        Assert.Null(user.LanguageCode);
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("FR", "fr")]
    [InlineData(" it ", "it")]
    public void SetLanguage_SupportedCode_IsNormalizedAndStored(string input, string expected)
    {
        var user = CreateUser();

        user.SetLanguage(input);

        Assert.Equal(expected, user.LanguageCode);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("de-CH")]
    [InlineData("German")]
    public void SetLanguage_UnsupportedCode_FallsBackToEnglishRatherThanBeingRejected(string input)
    {
        var user = CreateUser();

        user.SetLanguage(input);

        Assert.Equal("en", user.LanguageCode);
    }

    [Fact]
    public void SetLanguage_Null_ClearsAnyExistingPreference()
    {
        var user = CreateUser();
        user.SetLanguage("de");

        user.SetLanguage(null);

        Assert.Null(user.LanguageCode);
    }
}
