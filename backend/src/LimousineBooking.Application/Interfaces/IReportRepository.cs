using LimousineBooking.Application.Reports;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// All reporting queries — every method aggregates in the database (GroupBy/
/// Sum/Count/Average) and returns a small, already-grouped result. Nothing here
/// loads whole booking tables into memory or issues one query per driver/vehicle.
/// </summary>
public interface IReportRepository
{
    Task<BookingCreatedAggregate> GetBookingCreatedAggregateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    Task<int> GetCancelledByCancelledAtAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    Task<CompletedAggregate> GetCompletedByCompletionDateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    Task<AssignmentCountAggregate> GetAssignmentCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    /// <summary>Payment attempts created in range, grouped by their CURRENT status — Pending/Processing are reported together as "in flight".</summary>
    Task<PaymentAggregate> GetPaymentAggregateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenueByDayItem>> GetRevenueByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingsByDayItem>> GetBookingsByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    /// <summary>Grouped by RouteId, joined to the route's current name — PercentageOfTotalBookings is left at 0 for the caller to fill in.</summary>
    Task<IReadOnlyList<PopularRouteItem>> GetRouteAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    /// <summary>Every driver's id + display name — not paginated, since a report must include drivers with zero bookings.</summary>
    Task<IReadOnlyList<DriverNameRow>> GetAllDriversAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverRangeRow>> GetDriverRangeAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnerCountRow>> GetDriverUpcomingCountsAsync(DateOnly todayLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnerCountRow>> GetDriverManualAssignmentCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    /// <summary>Distinct (driver, booking) pairs from AssignmentHistory where the booking was cancelled and its CancelledAt falls in range, grouped by driver.</summary>
    Task<IReadOnlyList<OwnerCountRow>> GetDriverCancelledCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);

    /// <summary>Every vehicle's id + description — not paginated, since a report must include vehicles with zero bookings.</summary>
    Task<IReadOnlyList<VehicleNameRow>> GetAllVehiclesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleRangeRow>> GetVehicleRangeAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnerCountRow>> GetVehicleUpcomingCountsAsync(DateOnly todayLocal, CancellationToken cancellationToken = default);

    Task<PassengerAggregate> GetPassengerAggregateAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    /// <summary>Raw per-status counts (Pending/Confirmed/Completed/Cancelled only) for bookings whose TravelDate falls in range.</summary>
    Task<IReadOnlyDictionary<string, int>> GetStatusCountsAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<AssignmentBaseAggregate> GetAssignmentBaseAggregateAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnassignedBookingItem>> GetUnassignedBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetUnassignedCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpcomingOperationItem>> GetUpcomingOperationsAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<int> GetCancelledCountInRangeAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<int> GetTotalCountInRangeAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CancellationsByRouteItem>> GetCancellationsByRouteAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CancellationsByDayItem>> GetCancellationsByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CancellationReasonItem>> GetCancellationsByReasonAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default);

    /// <summary>Rows for the bookings CSV export, capped at <paramref name="maxRows"/> — a genuine streaming export is future work (section 48).</summary>
    Task<IReadOnlyList<BookingReportRow>> GetBookingReportRowsAsync(DateOnly fromLocal, DateOnly toLocal, int maxRows, CancellationToken cancellationToken = default);
}
