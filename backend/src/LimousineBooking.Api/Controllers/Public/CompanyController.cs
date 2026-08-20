using LimousineBooking.Application.Company;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Anonymous public company identity/contact information, for the website's header/footer/Contact page. No authentication required.</summary>
[ApiController]
[Route("api/public/company")]
[AllowAnonymous]
public class CompanyController : ControllerBase
{
    private readonly IPublicCompanyService _publicCompanyService;

    public CompanyController(IPublicCompanyService publicCompanyService)
    {
        _publicCompanyService = publicCompanyService;
    }

    [HttpGet]
    public ActionResult<CompanyInfoResponse> GetCompanyInfo()
    {
        return Ok(_publicCompanyService.GetCompanyInfo());
    }
}
