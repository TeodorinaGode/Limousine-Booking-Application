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
            Description = "A test description."
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
    }

    [Fact]
    public void GetCompanyInfo_EmptyOptionalFields_MapToNull()
    {
        var settings = new CompanySettings { EmergencyPhone = "", Description = "   " };
        var service = new PublicCompanyService(Options.Create(settings));

        var result = service.GetCompanyInfo();

        Assert.Null(result.EmergencyPhone);
        Assert.Null(result.Description);
    }
}
