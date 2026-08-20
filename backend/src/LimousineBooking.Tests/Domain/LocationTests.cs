using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class LocationTests
{
    private static Location MakeLocation() =>
        new("Basel", "CH", 47.5596, 7.5886, LocationType.City, "Major Swiss city", 1);

    [Fact]
    public void NewLocation_IsActiveByDefault()
    {
        var location = MakeLocation();

        Assert.True(location.IsActive);
    }

    [Fact]
    public void Constructor_UppercasesCountryCode()
    {
        var location = new Location("Basel", "ch", 47.5596, 7.5886, LocationType.City, null, 1);

        Assert.Equal("CH", location.CountryCode);
    }

    [Fact]
    public void Constructor_MissingName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Location("", "CH", 47.5596, 7.5886, LocationType.City, null, 1));
    }

    [Theory]
    [InlineData("C")]
    [InlineData("CHE")]
    [InlineData("")]
    public void Constructor_InvalidCountryCode_Throws(string countryCode)
    {
        Assert.Throws<ArgumentException>(() => new Location("Basel", countryCode, 47.5596, 7.5886, LocationType.City, null, 1));
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Constructor_LatitudeOutOfRange_Throws(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location("Basel", "CH", latitude, 7.5886, LocationType.City, null, 1));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Constructor_LongitudeOutOfRange_Throws(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location("Basel", "CH", 47.5596, longitude, LocationType.City, null, 1));
    }

    [Fact]
    public void Constructor_BlankDescription_IsStoredAsNull()
    {
        var location = new Location("Basel", "CH", 47.5596, 7.5886, LocationType.City, "   ", 1);

        Assert.Null(location.Description);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var location = MakeLocation();

        location.Deactivate();
        Assert.False(location.IsActive);

        location.Activate();
        Assert.True(location.IsActive);
    }
}
