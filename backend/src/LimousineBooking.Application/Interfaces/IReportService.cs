using LimousineBooking.Application.Common;
using LimousineBooking.Application.Reports;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Administrator reporting/analytics — every method is read-only and never mutates a booking (section 55).</summary>
public interface IReportService
{
    Task<ReportSummaryResponse> GetSummaryAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenueByDayItem>> GetRevenueByDayAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingsByDayItem>> GetBookingsByDayAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PopularRouteItem>> GetPopularRoutesAsync(ResolvedReportDateRange range, int? top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverActivityItem>> GetDriverActivityAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleUsageItem>> GetVehicleUsageAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<PassengerReportResponse> GetPassengerReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingStatusDistributionItem>> GetStatusDistributionAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<AssignmentReportResponse> GetAssignmentReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<PaymentReportResponse> GetPaymentReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnassignedBookingItem>> GetUnassignedBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpcomingOperationItem>> GetUpcomingOperationsAsync(string? period, CancellationToken cancellationToken = default);

    Task<CancellationReportResponse> GetCancellationReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default);

    /// <summary>CSV text for GET /api/admin/reports/bookings/export — capped server-side (never the browser dumping an already-loaded page, section 32). Column headers are localized per <paramref name="languageCode"/> (section 44); the data values themselves are never translated.</summary>
    Task<string> ExportBookingsCsvAsync(ResolvedReportDateRange range, string languageCode, CancellationToken cancellationToken = default);

    Task<string> ExportRoutesCsvAsync(ResolvedReportDateRange range, int? top, string languageCode, CancellationToken cancellationToken = default);

    Task<string> ExportDriversCsvAsync(ResolvedReportDateRange range, string languageCode, CancellationToken cancellationToken = default);

    Task<string> ExportVehiclesCsvAsync(ResolvedReportDateRange range, string languageCode, CancellationToken cancellationToken = default);
}
