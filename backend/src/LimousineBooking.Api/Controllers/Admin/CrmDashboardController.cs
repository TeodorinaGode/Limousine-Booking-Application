using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>CRM Overview — a current-state snapshot, no date filter. See CrmReportsController for date-ranged analysis.</summary>
[ApiController]
[Route("api/admin/crm/dashboard")]
[Authorize(Roles = "Administrator")]
public class CrmDashboardController : ControllerBase
{
    private readonly ICrmAnalyticsService _analyticsService;

    public CrmDashboardController(ICrmAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<CrmDashboardResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _analyticsService.GetDashboardAsync(cancellationToken));
}
