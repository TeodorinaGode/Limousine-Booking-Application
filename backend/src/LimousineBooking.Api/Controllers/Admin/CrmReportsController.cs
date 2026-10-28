using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>CRM date-ranged analysis — shares the dateFrom/dateTo convention with the booking Reports module (ReportDateRangeResolver).</summary>
[ApiController]
[Route("api/admin/crm/reports")]
[Authorize(Roles = "Administrator")]
public class CrmReportsController : ControllerBase
{
    private readonly ICrmAnalyticsService _analyticsService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CrmReportsController(ICrmAnalyticsService analyticsService, IDateTimeProvider dateTimeProvider)
    {
        _analyticsService = analyticsService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CrmReportSummaryResponse>> GetSummary([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        var (range, error) = ReportDateRangeResolver.Resolve(query.DateFrom, query.DateTo, _dateTimeProvider.UtcNow);
        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(await _analyticsService.GetReportSummaryAsync(range!, cancellationToken));
    }
}
