using LimousineBooking.Application.Bookings;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> ReferenceExistsAsync(string bookingReference, CancellationToken cancellationToken = default);

    /// <summary>Includes Route, Driver (+ Driver.User), and Vehicle — everything the assignment service and admin endpoints need without a second round trip.</summary>
    Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Booking> Items, int TotalCount)> SearchAsync(AdminBookingSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>Counts for the admin dashboard — each a targeted COUNT query, never a full table load.</summary>
    Task<AdminBookingCounts> GetDashboardCountsAsync(DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>The next <paramref name="count"/> upcoming Pending/Confirmed bookings (today or later), soonest first.</summary>
    Task<IReadOnlyList<Booking>> GetUpcomingAsync(DateOnly fromDate, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Non-cancelled bookings on <paramref name="date"/> whose DriverId or VehicleId
    /// is among the given candidate sets (excluding <paramref name="excludeBookingId"/>
    /// itself), with Route included so callers can compute each booking's end time.
    /// Used by the assignment service to check for scheduling conflicts.
    /// </summary>
    Task<IReadOnlyList<Booking>> GetConflictScanAsync(
        DateOnly date,
        IReadOnlyCollection<Guid> driverIds,
        IReadOnlyCollection<Guid> vehicleIds,
        Guid excludeBookingId,
        CancellationToken cancellationToken = default);

    /// <summary>Count of non-cancelled bookings on or after <paramref name="fromDate"/>, per driver — used to rank candidates by workload.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetUpcomingBookingCountsAsync(
        IReadOnlyCollection<Guid> driverIds,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
