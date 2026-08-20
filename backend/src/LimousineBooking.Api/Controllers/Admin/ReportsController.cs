using System.Text;
using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only reporting/analytics — every endpoint is read-only (never
/// mutates a booking) and shares the one dateFrom/dateTo filter convention
/// resolved by ReportDateRangeResolver (default: current Europe/Zurich month).
/// </summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Administrator")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReportsController(IReportService reportService, IDateTimeProvider dateTimeProvider)
    {
        _reportService = reportService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryResponse>> GetSummary([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetSummaryAsync(range, cancellationToken));
    }

    [HttpGet("revenue-by-day")]
    public async Task<ActionResult<IReadOnlyList<RevenueByDayItem>>> GetRevenueByDay([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetRevenueByDayAsync(range, cancellationToken));
    }

    [HttpGet("bookings-by-day")]
    public async Task<ActionResult<IReadOnlyList<BookingsByDayItem>>> GetBookingsByDay([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetBookingsByDayAsync(range, cancellationToken));
    }

    /// <summary>Popular routes, sorted by booking count descending. <c>top</c>: 5, 10 (default), 20, or omitted/0 for all (section 14).</summary>
    [HttpGet("routes")]
    public async Task<ActionResult<IReadOnlyList<PopularRouteItem>>> GetRoutes([FromQuery] RouteReportQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetPopularRoutesAsync(range, query.Top, cancellationToken));
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<IReadOnlyList<DriverActivityItem>>> GetDrivers([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetDriverActivityAsync(range, cancellationToken));
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<IReadOnlyList<VehicleUsageItem>>> GetVehicles([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetVehicleUsageAsync(range, cancellationToken));
    }

    [HttpGet("passengers")]
    public async Task<ActionResult<PassengerReportResponse>> GetPassengers([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetPassengerReportAsync(range, cancellationToken));
    }

    [HttpGet("status-distribution")]
    public async Task<ActionResult<IReadOnlyList<BookingStatusDistributionItem>>> GetStatusDistribution([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetStatusDistributionAsync(range, cancellationToken));
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<AssignmentReportResponse>> GetAssignments([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetAssignmentReportAsync(range, cancellationToken));
    }

    /// <summary>Bookings currently requiring manual assignment — a current-state view, not scoped to a date filter (section 25).</summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<IReadOnlyList<UnassignedBookingItem>>> GetUnassigned([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        return Ok(await _reportService.GetUnassignedBookingsAsync(page, pageSize, cancellationToken));
    }

    /// <summary>Upcoming trips. <c>period</c>: today | next7 (default) | next30 — its own selector, independent of the report date filter (section 26).</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<UpcomingOperationItem>>> GetUpcoming([FromQuery] string? period, CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetUpcomingOperationsAsync(period, cancellationToken));
    }

    [HttpGet("cancellations")]
    public async Task<ActionResult<CancellationReportResponse>> GetCancellations([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return Ok(await _reportService.GetCancellationReportAsync(range, cancellationToken));
    }

    [HttpGet("bookings/export")]
    public async Task<IActionResult> ExportBookings([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return CsvFile(await _reportService.ExportBookingsCsvAsync(range, cancellationToken), "bookings-report.csv");
    }

    [HttpGet("routes/export")]
    public async Task<IActionResult> ExportRoutes([FromQuery] RouteReportQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return CsvFile(await _reportService.ExportRoutesCsvAsync(range, query.Top, cancellationToken), "routes-report.csv");
    }

    [HttpGet("drivers/export")]
    public async Task<IActionResult> ExportDrivers([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return CsvFile(await _reportService.ExportDriversCsvAsync(range, cancellationToken), "drivers-report.csv");
    }

    [HttpGet("vehicles/export")]
    public async Task<IActionResult> ExportVehicles([FromQuery] ReportDateRangeQuery query, CancellationToken cancellationToken)
    {
        if (!TryResolveRange(query, out var range, out var problem))
            return problem;

        return CsvFile(await _reportService.ExportVehiclesCsvAsync(range, cancellationToken), "vehicles-report.csv");
    }

    private FileContentResult CsvFile(string csv, string fileName) =>
        File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);

    private bool TryResolveRange(ReportDateRangeQuery query, out ResolvedReportDateRange range, out ActionResult problem)
    {
        var (resolved, error) = ReportDateRangeResolver.Resolve(query.DateFrom, query.DateTo, _dateTimeProvider.UtcNow);
        if (error is not null)
        {
            range = null!;
            problem = BadRequest(new { message = error });
            return false;
        }

        range = resolved!;
        problem = null!;
        return true;
    }
}
