using LimousineBooking.Application.Company;
using Microsoft.Extensions.Options;
using Xunit;

namespace LimousineBooking.Tests.Company;

public class PublicCompanyServiceTests
{
    [Fact]
    public void GetCompanyInfo_MapsAllConfiguredFields()
    {
        var settings = new CompanySettings
        {
            CompanyName = "Test Chauffeur",
            Tagline = "Test Tagline",
            Phone = "+41 79 000 00 00",
            Email = "test@example.com",
            Address = "Bahnhofplatz 1, Basel",
            Website = "https://example.com",
            OpeningHours = "Mon-Fri, 08:00-18:00",
            EmergencyPhone = "+41 79 111 11 11",
            Description = "A test description.",
            OperatingCountryCodes = new List<string> { "CH", "AT" },
            FacebookUrl = "https://facebook.com/test",
            InstagramUrl = "https://instagram.com/test",
            WhatsAppUrl = "https://wa.me/41790000000"
        };
        var service = new PublicCompanyService(Options.Create(settings));

        var result = service.GetCompanyInfo();

        Assert.Equal("Test Chauffeur", result.CompanyName);
        Assert.Equal("Test Tagline", result.Tagline);
        Assert.Equal("+41 79 000 00 00", result.Phone);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Bahnhofplatz 1, Basel", result.Address);
        Assert.Equal("https://example.com", result.Website);
        Assert.Equal("Mon-Fri, 08:00-18:00", result.OpeningHours);
        Assert.Equal("+41 79 111 11 11", result.EmergencyPhone);
        Assert.Equal("A test description.", result.Description);
        Assert.Equal(new[] { "CH", "AT" }, result.OperatingCountryCodes);
        Assert.Equal("https://facebook.com/test", result.FacebookUrl);
        Assert.Equal("https://instagram.com/test", result.InstagramUrl);
        Assert.Equal("https://wa.me/41790000000", result.WhatsAppUrl);
    }

    [Fact]
    public void GetCompanyInfo_EmptyOptionalFields_MapToNull()
    {
        var settings = new CompanySettings { EmergencyPhone = "", Description = "   ", FacebookUrl = "", InstagramUrl = "", WhatsAppUrl = "" };
        var service = new PublicCompanyService(Options.Create(settings));

        var result = service.GetCompanyInfo();

        Assert.Null(result.EmergencyPhone);
        Assert.Null(result.Description);
        Assert.Null(result.FacebookUrl);
        Assert.Null(result.InstagramUrl);
        Assert.Null(result.WhatsAppUrl);
    }
}
