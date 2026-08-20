using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Company;

/// <inheritdoc cref="IPublicCompanyService" />
public class PublicCompanyService : IPublicCompanyService
{
    private readonly CompanySettings _settings;

    public PublicCompanyService(IOptions<CompanySettings> settings)
    {
        _settings = settings.Value;
    }

    public CompanyInfoResponse GetCompanyInfo() => new()
    {
        CompanyName = _settings.CompanyName,
        Tagline = _settings.Tagline,
        Phone = _settings.Phone,
        Email = _settings.Email,
        Address = _settings.Address,
        Website = _settings.Website,
        OpeningHours = _settings.OpeningHours,
        EmergencyPhone = string.IsNullOrWhiteSpace(_settings.EmergencyPhone) ? null : _settings.EmergencyPhone,
        Description = string.IsNullOrWhiteSpace(_settings.Description) ? null : _settings.Description
    };
}
