using LimousineBooking.Domain.Common;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("DE")]
    [InlineData(" fr ")]
    public void Normalize_SupportedCode_ReturnsItLowercasedAndTrimmed(string code)
    {
        var result = SupportedLanguages.Normalize(code);

        Assert.Contains(result, SupportedLanguages.Codes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("de-CH")]
    [InlineData("German")]
    public void Normalize_UnsupportedOrMissingCode_FallsBackToEnglish(string? code)
    {
        Assert.Equal("en", SupportedLanguages.Normalize(code));
    }

    [Theory]
    [InlineData("en", true)]
    [InlineData("fr", true)]
    [InlineData("es", false)]
    [InlineData("it", false)]
    [InlineData(null, false)]
    public void IsSupported_ReflectsTheThreeLanguageAllowList(string? code, bool expected)
    {
        Assert.Equal(expected, SupportedLanguages.IsSupported(code));
    }
}
