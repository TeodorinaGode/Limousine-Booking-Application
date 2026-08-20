using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BookingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ReferenceExistsAsync(string bookingReference, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.AnyAsync(b => b.BookingReference == bookingReference, cancellationToken);

    public Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings
            .Include(b => b.Route)
            .Include(b => b.Driver).ThenInclude(d => d!.User)
            .Include(b => b.Vehicle)
            .Include(b => b.Payments)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Booking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings
            .Include(b => b.Route)
            .SingleOrDefaultAsync(b => b.BookingReference == bookingReference, cancellationToken);

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> SearchAsync(AdminBookingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var bookings = _dbContext.Bookings
            .Include(b => b.Route)
            .Include(b => b.Driver).ThenInclude(d => d!.User)
            .Include(b => b.Vehicle)
            .Include(b => b.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            bookings = bookings.Where(b =>
                EF.Functions.ILike(b.BookingReference, pattern) ||
                EF.Functions.ILike(b.CustomerFirstName, pattern) ||
                EF.Functions.ILike(b.CustomerLastName, pattern) ||
                EF.Functions.ILike(b.CustomerEmail, pattern) ||
                EF.Functions.ILike(b.CustomerPhone, pattern));
        }

        var statuses = ParseStatuses(query.Status);
        if (statuses.Count > 0)
            bookings = bookings.Where(b => statuses.Contains(b.Status));

        if (query.DateFrom.HasValue)
            bookings = bookings.Where(b => b.TravelDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            bookings = bookings.Where(b => b.TravelDate <= query.DateTo.Value);

        if (query.DriverId.HasValue)
            bookings = bookings.Where(b => b.DriverId == query.DriverId.Value);
        if (query.VehicleId.HasValue)
            bookings = bookings.Where(b => b.VehicleId == query.VehicleId.Value);
        if (query.RouteId.HasValue)
            bookings = bookings.Where(b => b.RouteId == query.RouteId.Value);

        bookings = query.AssignmentFilter?.ToLowerInvariant() switch
        {
            "automatic" => bookings.Where(b => b.AssignmentType == AssignmentType.Automatic),
            "manual" => bookings.Where(b => b.AssignmentType == AssignmentType.Manual),
            "requiresmanual" => bookings.Where(b => b.RequiresManualAssignment),
            _ => bookings
        };

        // "notStarted" = no payment attempt exists yet; a named status = at least
        // one attempt reached that status (a booking with a Paid attempt is never
        // also matched by "failed", since MarkPaid/MarkFailed are mutually exclusive
        // terminal states on a single attempt — see Payment's transition guards).
        if (!string.IsNullOrWhiteSpace(query.PaymentStatus) && !string.Equals(query.PaymentStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            bookings = query.PaymentStatus.ToLowerInvariant() == "notstarted"
                ? bookings.Where(b => !b.Payments.Any())
                : Enum.TryParse<PaymentStatus>(query.PaymentStatus, ignoreCase: true, out var paymentStatus)
                    ? bookings.Where(b => b.Payments.Any(p => p.Status == paymentStatus))
                    : bookings;
        }

        var totalCount = await bookings.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        bookings = query.SortBy?.ToLowerInvariant() switch
        {
            "pickuptime" => descending ? bookings.OrderByDescending(b => b.PickupTime) : bookings.OrderBy(b => b.PickupTime),
            "createdat" => descending ? bookings.OrderByDescending(b => b.CreatedAt) : bookings.OrderBy(b => b.CreatedAt),
            "customername" => descending
                ? bookings.OrderByDescending(b => b.CustomerFirstName).ThenByDescending(b => b.CustomerLastName)
                : bookings.OrderBy(b => b.CustomerFirstName).ThenBy(b => b.CustomerLastName),
            "status" => descending ? bookings.OrderByDescending(b => b.Status) : bookings.OrderBy(b => b.Status),
            // "bookingDate" and any unrecognized value fall back to the default —
            // upcoming trips first (date then pickup time, both ascending).
            _ => descending
                ? bookings.OrderByDescending(b => b.TravelDate).ThenByDescending(b => b.PickupTime)
                : bookings.OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
        };

        var items = await bookings
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Booking?> GetByDriverAndIdAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings
            .Include(b => b.Route)
            .SingleOrDefaultAsync(b => b.Id == bookingId && b.DriverId == driverId, cancellationToken);

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> SearchByDriverAsync(Guid driverId, DriverBookingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var bookings = _dbContext.Bookings
            .Include(b => b.Route)
            .Where(b => b.DriverId == driverId)
            .AsQueryable();

        if (query.DateFrom.HasValue)
            bookings = bookings.Where(b => b.TravelDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            bookings = bookings.Where(b => b.TravelDate <= query.DateTo.Value);

        var totalCount = await bookings.CountAsync(cancellationToken);

        var items = await bookings
            .OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Booking>> GetByDriverAndDateAsync(Guid driverId, DateOnly date, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Where(b => b.DriverId == driverId && b.TravelDate == date)
            .OrderBy(b => b.PickupTime)
            .ToListAsync(cancellationToken);

    public Task<int> CountUpcomingByDriverAsync(Guid driverId, DateOnly afterDate, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.CountAsync(b => b.DriverId == driverId && b.TravelDate > afterDate, cancellationToken);

    public async Task<AdminBookingCounts> GetDashboardCountsAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var statusCounts = await _dbContext.Bookings
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = statusCounts.ToDictionary(s => s.Status, s => s.Count);

        return new AdminBookingCounts
        {
            TotalBookings = byStatus.Values.Sum(),
            TodaysBookings = await _dbContext.Bookings.CountAsync(b => b.TravelDate == today, cancellationToken),
            PendingBookings = byStatus.GetValueOrDefault(BookingStatus.Pending),
            RequiresManualAssignmentCount = await _dbContext.Bookings.CountAsync(b => b.RequiresManualAssignment, cancellationToken),
            ConfirmedBookings = byStatus.GetValueOrDefault(BookingStatus.Confirmed),
            CancelledBookings = byStatus.GetValueOrDefault(BookingStatus.Cancelled),
            UpcomingTripsCount = await _dbContext.Bookings.CountAsync(
                b => b.TravelDate >= today && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
                cancellationToken)
        };
    }

    public async Task<IReadOnlyList<Booking>> GetUpcomingAsync(DateOnly fromDate, int count, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Include(b => b.Driver).ThenInclude(d => d!.User)
            .Include(b => b.Vehicle)
            .Where(b => b.TravelDate >= fromDate && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetConflictScanAsync(
        DateOnly date,
        IReadOnlyCollection<Guid> driverIds,
        IReadOnlyCollection<Guid> vehicleIds,
        Guid excludeBookingId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Where(b =>
                b.TravelDate == date &&
                b.Id != excludeBookingId &&
                b.Status != BookingStatus.Cancelled &&
                ((b.DriverId != null && driverIds.Contains(b.DriverId.Value)) ||
                 (b.VehicleId != null && vehicleIds.Contains(b.VehicleId.Value))))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUpcomingBookingCountsAsync(
        IReadOnlyCollection<Guid> driverIds,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.Bookings
            .Where(b =>
                b.DriverId != null &&
                driverIds.Contains(b.DriverId.Value) &&
                b.Status != BookingStatus.Cancelled &&
                b.TravelDate >= fromDate)
            .GroupBy(b => b.DriverId!.Value)
            .Select(g => new { DriverId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.DriverId, c => c.Count);
    }

    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _dbContext.Bookings.Add(booking);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private static List<BookingStatus> ParseStatuses(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<BookingStatus>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.TryParse<BookingStatus>(s, ignoreCase: true, out var status) ? status : (BookingStatus?)null)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .Distinct()
            .ToList();
    }
}
