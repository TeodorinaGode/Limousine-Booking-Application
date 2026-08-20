using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Reports;

/// <summary>
/// Composes IReportRepository's grouped query results into the final report DTOs
/// — percentages/rates computed safely (0 instead of NaN/Infinity on an empty
/// denominator), and driver/vehicle activity zero-filled so a driver/vehicle with
/// no bookings in range still appears (section 59/60). Every method is read-only.
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReportService(IReportRepository reportRepository, IDateTimeProvider dateTimeProvider)
    {
        _reportRepository = reportRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ReportSummaryResponse> GetSummaryAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var created = await _reportRepository.GetBookingCreatedAggregateAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);
        var cancelled = await _reportRepository.GetCancelledByCancelledAtAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);
        var completed = await _reportRepository.GetCompletedByCompletionDateAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);
        var assignments = await _reportRepository.GetAssignmentCountsAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);

        return new ReportSummaryResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            TotalBookings = created.Total,
            ConfirmedBookings = created.Confirmed,
            PendingBookings = created.Pending,
            CompletedBookings = completed.Count,
            CancelledBookings = cancelled,
            GrossRevenue = created.GrossRevenue,
            CompletedRevenue = completed.Revenue,
            AverageBookingValue = SafeDivide(created.GrossRevenue, created.Total),
            AverageCompletedBookingValue = SafeDivide(completed.Revenue, completed.Count),
            ManualAssignments = assignments.Manual,
            AutomaticAssignments = assignments.Automatic
        };
    }

    public Task<IReadOnlyList<RevenueByDayItem>> GetRevenueByDayAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default) =>
        _reportRepository.GetRevenueByDayAsync(range.FromLocal, range.ToLocal, cancellationToken);

    public Task<IReadOnlyList<BookingsByDayItem>> GetBookingsByDayAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default) =>
        _reportRepository.GetBookingsByDayAsync(range.FromLocal, range.ToLocal, cancellationToken);

    public async Task<IReadOnlyList<PopularRouteItem>> GetPopularRoutesAsync(ResolvedReportDateRange range, int? top, CancellationToken cancellationToken = default)
    {
        var routes = await _reportRepository.GetRouteAggregatesAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var totalBookings = routes.Sum(r => r.BookingCount);

        foreach (var route in routes)
            route.PercentageOfTotalBookings = SafePercentage(route.BookingCount, totalBookings);

        // 5, 10 (default), 20, or "all" for null/0/negative (section 14).
        var limit = top is > 0 ? top.Value : (top == null ? 10 : routes.Count);
        return routes.Take(limit).ToList();
    }

    public async Task<IReadOnlyList<DriverActivityItem>> GetDriverActivityAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow));

        var drivers = await _reportRepository.GetAllDriversAsync(cancellationToken);
        var rangeRows = (await _reportRepository.GetDriverRangeAggregatesAsync(range.FromLocal, range.ToLocal, cancellationToken)).ToDictionary(r => r.DriverId);
        var upcoming = (await _reportRepository.GetDriverUpcomingCountsAsync(todayLocal, cancellationToken)).ToDictionary(r => r.OwnerId, r => r.Count);
        var manual = (await _reportRepository.GetDriverManualAssignmentCountsAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken)).ToDictionary(r => r.OwnerId, r => r.Count);
        var cancelledCounts = (await _reportRepository.GetDriverCancelledCountsAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken)).ToDictionary(r => r.OwnerId, r => r.Count);

        return drivers.Select(d =>
        {
            rangeRows.TryGetValue(d.DriverId, out var driverRange);
            var assigned = driverRange?.Assigned ?? 0;
            var completed = driverRange?.Completed ?? 0;

            return new DriverActivityItem
            {
                DriverId = d.DriverId,
                DriverName = d.Name,
                AssignedBookings = assigned,
                CompletedRides = completed,
                CancelledBookings = cancelledCounts.GetValueOrDefault(d.DriverId),
                UpcomingBookings = upcoming.GetValueOrDefault(d.DriverId),
                ManualAssignments = manual.GetValueOrDefault(d.DriverId),
                CompletionRate = SafePercentage(completed, assigned)
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<VehicleUsageItem>> GetVehicleUsageAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow));

        var vehicles = await _reportRepository.GetAllVehiclesAsync(cancellationToken);
        var rangeRows = (await _reportRepository.GetVehicleRangeAggregatesAsync(range.FromLocal, range.ToLocal, cancellationToken)).ToDictionary(r => r.VehicleId);
        var upcoming = (await _reportRepository.GetVehicleUpcomingCountsAsync(todayLocal, cancellationToken)).ToDictionary(r => r.OwnerId, r => r.Count);

        return vehicles.Select(v =>
        {
            rangeRows.TryGetValue(v.VehicleId, out var vehicleRange);
            var assigned = vehicleRange?.Assigned ?? 0;

            return new VehicleUsageItem
            {
                VehicleId = v.VehicleId,
                VehicleDescription = v.Description,
                AssignedBookings = assigned,
                CompletedRides = vehicleRange?.Completed ?? 0,
                UpcomingBookings = upcoming.GetValueOrDefault(v.VehicleId),
                TotalPassengers = vehicleRange?.Passengers ?? 0,
                Utilization = assigned
            };
        }).ToList();
    }

    public async Task<PassengerReportResponse> GetPassengerReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var agg = await _reportRepository.GetPassengerAggregateAsync(range.FromLocal, range.ToLocal, cancellationToken);

        return new PassengerReportResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            TotalPassengers = agg.TotalPassengers,
            AveragePassengersPerBooking = SafeDivide(agg.TotalPassengers, agg.BookingCount),
            MaximumPassengersInABooking = agg.MaximumPassengers
        };
    }

    public async Task<IReadOnlyList<BookingStatusDistributionItem>> GetStatusDistributionAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var counts = await _reportRepository.GetStatusCountsAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var total = counts.Values.Sum();

        string[] statuses = { "Pending", "Confirmed", "Completed", "Cancelled" };
        return statuses.Select(s =>
        {
            var count = counts.GetValueOrDefault(s);
            return new BookingStatusDistributionItem { Status = s, Count = count, Percentage = SafePercentage(count, total) };
        }).ToList();
    }

    public async Task<AssignmentReportResponse> GetAssignmentReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var assignments = await _reportRepository.GetAssignmentCountsAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);
        var baseAgg = await _reportRepository.GetAssignmentBaseAggregateAsync(range.FromLocal, range.ToLocal, cancellationToken);

        return new AssignmentReportResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            AutomaticAssignments = assignments.Automatic,
            ManualAssignments = assignments.Manual,
            RequiresManualAssignment = baseAgg.RequiresManual,
            ManualAssignmentRate = SafePercentage(assignments.Manual, assignments.Manual + assignments.Automatic),
            AssignmentSuccessRate = SafePercentage(baseAgg.Assigned, baseAgg.TotalNonCancelled)
        };
    }

    public async Task<PaymentReportResponse> GetPaymentReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var agg = await _reportRepository.GetPaymentAggregateAsync(range.FromUtc, range.ToUtcExclusive, cancellationToken);

        return new PaymentReportResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            TotalPaymentAttempts = agg.Total,
            SuccessfulPayments = agg.Successful,
            FailedPayments = agg.Failed,
            PendingPayments = agg.Pending,
            CancelledPayments = agg.Cancelled,
            RefundedPayments = agg.Refunded,
            PaidRevenue = agg.PaidRevenue,
            RefundedAmount = agg.RefundedAmount
        };
    }

    public Task<IReadOnlyList<UnassignedBookingItem>> GetUnassignedBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _reportRepository.GetUnassignedBookingsAsync(page, pageSize, cancellationToken);

    public Task<IReadOnlyList<UpcomingOperationItem>> GetUpcomingOperationsAsync(string? period, CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow));

        var toLocal = period?.ToLowerInvariant() switch
        {
            "today" => todayLocal,
            "next30" => todayLocal.AddDays(30),
            _ => todayLocal.AddDays(7)
        };

        return _reportRepository.GetUpcomingOperationsAsync(todayLocal, toLocal, cancellationToken);
    }

    public async Task<CancellationReportResponse> GetCancellationReportAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var cancelledCount = await _reportRepository.GetCancelledCountInRangeAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var totalCount = await _reportRepository.GetTotalCountInRangeAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var byRoute = await _reportRepository.GetCancellationsByRouteAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var byDay = await _reportRepository.GetCancellationsByDayAsync(range.FromLocal, range.ToLocal, cancellationToken);
        var byReason = await _reportRepository.GetCancellationsByReasonAsync(range.FromLocal, range.ToLocal, cancellationToken);

        return new CancellationReportResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            TotalCancellations = cancelledCount,
            TotalBookings = totalCount,
            CancellationRate = SafePercentage(cancelledCount, totalCount),
            CancellationsByRoute = byRoute,
            CancellationsByDay = byDay,
            CancellationsByReason = byReason
        };
    }

    private const int MaxExportRows = 10_000;

    public async Task<string> ExportBookingsCsvAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var rows = await _reportRepository.GetBookingReportRowsAsync(range.FromLocal, range.ToLocal, MaxExportRows, cancellationToken);

        return CsvWriter.Write(
            new[] { "Booking Reference", "Date", "Time", "Route", "Customer", "Driver", "Vehicle", "Passengers", "Price", "Currency", "Status", "Ride Status" },
            rows.Select(r => new[]
            {
                r.BookingReference,
                CsvWriter.Format(r.Date),
                CsvWriter.Format(r.Time),
                r.Route,
                r.Customer,
                r.Driver ?? string.Empty,
                r.Vehicle ?? string.Empty,
                r.Passengers.ToString(),
                CsvWriter.Format(r.Price),
                r.Currency,
                r.Status,
                r.RideStatus
            }));
    }

    public async Task<string> ExportRoutesCsvAsync(ResolvedReportDateRange range, int? top, CancellationToken cancellationToken = default)
    {
        var routes = await GetPopularRoutesAsync(range, top, cancellationToken);

        return CsvWriter.Write(
            new[] { "Departure", "Destination", "Booking Count", "Revenue", "Percentage Of Total Bookings" },
            routes.Select(r => new[]
            {
                r.DepartureLocation,
                r.Destination,
                r.BookingCount.ToString(),
                CsvWriter.Format(r.Revenue),
                CsvWriter.Format(r.PercentageOfTotalBookings)
            }));
    }

    public async Task<string> ExportDriversCsvAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var drivers = await GetDriverActivityAsync(range, cancellationToken);

        return CsvWriter.Write(
            new[] { "Driver", "Assigned", "Completed", "Cancelled", "Upcoming", "Manual Assignments", "Completion Rate" },
            drivers.Select(d => new[]
            {
                d.DriverName,
                d.AssignedBookings.ToString(),
                d.CompletedRides.ToString(),
                d.CancelledBookings.ToString(),
                d.UpcomingBookings.ToString(),
                d.ManualAssignments.ToString(),
                CsvWriter.Format(d.CompletionRate)
            }));
    }

    public async Task<string> ExportVehiclesCsvAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var vehicles = await GetVehicleUsageAsync(range, cancellationToken);

        return CsvWriter.Write(
            new[] { "Vehicle", "Assigned", "Completed", "Upcoming", "Total Passengers", "Utilization (Booking Count)" },
            vehicles.Select(v => new[]
            {
                v.VehicleDescription,
                v.AssignedBookings.ToString(),
                v.CompletedRides.ToString(),
                v.UpcomingBookings.ToString(),
                v.TotalPassengers.ToString(),
                v.Utilization.ToString()
            }));
    }

    private static decimal SafeDivide(decimal numerator, int denominator) =>
        denominator == 0 ? 0m : Math.Round(numerator / denominator, 2);

    private static decimal SafePercentage(int numerator, int denominator) =>
        denominator == 0 ? 0m : Math.Round(numerator * 100m / denominator, 1);
}
