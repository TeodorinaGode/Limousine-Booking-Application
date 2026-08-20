using LimousineBooking.Application.Company;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Serves the public-facing company identity/contact information (Prompt 17, section 16/39).</summary>
public interface IPublicCompanyService
{
    CompanyInfoResponse GetCompanyInfo();
}
