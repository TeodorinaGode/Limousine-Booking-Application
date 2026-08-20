using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Reports;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReportRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BookingCreatedAggregate> GetBookingCreatedAggregateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Bookings
            .Where(b => b.CreatedAt >= fromUtc && b.CreatedAt < toUtcExclusive)
            .GroupBy(b => 1)
            .Select(g => new BookingCreatedAggregate
            {
                Total = g.Count(),
                Confirmed = g.Count(b => b.Status == BookingStatus.Confirmed),
                Pending = g.Count(b => b.Status == BookingStatus.Pending),
                GrossRevenue = g.Sum(b => b.Price)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new BookingCreatedAggregate();
    }

    public Task<int> GetCancelledByCancelledAtAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.CountAsync(
            b => b.CancelledAt != null && b.CancelledAt >= fromUtc && b.CancelledAt < toUtcExclusive,
            cancellationToken);

    public async Task<CompletedAggregate> GetCompletedByCompletionDateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.RideStatusHistories
            .Where(h => h.NewStatus == RideStatus.Completed && h.ChangedAt >= fromUtc && h.ChangedAt < toUtcExclusive)
            .Join(_dbContext.Bookings, h => h.BookingId, b => b.Id, (h, b) => b.Price)
            .GroupBy(p => 1)
            .Select(g => new CompletedAggregate { Count = g.Count(), Revenue = g.Sum(p => p) })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new CompletedAggregate();
    }

    public async Task<AssignmentCountAggregate> GetAssignmentCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.AssignmentHistories
            .Where(a => a.AssignedAt >= fromUtc && a.AssignedAt < toUtcExclusive)
            .GroupBy(a => 1)
            .Select(g => new AssignmentCountAggregate
            {
                Manual = g.Count(a => a.AssignmentType == AssignmentType.Manual),
                Automatic = g.Count(a => a.AssignmentType == AssignmentType.Automatic)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new AssignmentCountAggregate();
    }

    public async Task<PaymentAggregate> GetPaymentAggregateAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Payments
            .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtcExclusive)
            .GroupBy(p => 1)
            .Select(g => new PaymentAggregate
            {
                Total = g.Count(),
                Successful = g.Count(p => p.Status == PaymentStatus.Paid),
                Failed = g.Count(p => p.Status == PaymentStatus.Failed),
                Pending = g.Count(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing),
                Cancelled = g.Count(p => p.Status == PaymentStatus.Cancelled),
                Refunded = g.Count(p => p.Status == PaymentStatus.Refunded),
                PaidRevenue = g.Where(p => p.Status == PaymentStatus.Paid).Sum(p => (decimal?)p.Amount) ?? 0m,
                RefundedAmount = g.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new PaymentAggregate();
    }

    public async Task<IReadOnlyList<RevenueByDayItem>> GetRevenueByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => b.TravelDate)
            .Select(g => new RevenueByDayItem { Date = g.Key, BookingCount = g.Count(), Revenue = g.Sum(b => b.Price) })
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookingsByDayItem>> GetBookingsByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => b.TravelDate)
            .Select(g => new BookingsByDayItem
            {
                Date = g.Key,
                Total = g.Count(),
                Completed = g.Count(b => b.Status == BookingStatus.Completed),
                Cancelled = g.Count(b => b.Status == BookingStatus.Cancelled),
                Pending = g.Count(b => b.Status == BookingStatus.Pending),
                Confirmed = g.Count(b => b.Status == BookingStatus.Confirmed)
            })
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PopularRouteItem>> GetRouteAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => new { b.RouteId, b.Route!.DepartureLocation, b.Route.Destination })
            .Select(g => new PopularRouteItem
            {
                RouteId = g.Key.RouteId,
                DepartureLocation = g.Key.DepartureLocation,
                Destination = g.Key.Destination,
                BookingCount = g.Count(),
                Revenue = g.Sum(b => b.Price),
                PercentageOfTotalBookings = 0
            })
            .OrderByDescending(r => r.BookingCount)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DriverNameRow>> GetAllDriversAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Drivers
            .Select(d => new DriverNameRow { DriverId = d.Id, Name = d.User!.FirstName + " " + d.User.LastName })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DriverRangeRow>> GetDriverRangeAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.DriverId != null && b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => b.DriverId!.Value)
            .Select(g => new DriverRangeRow
            {
                DriverId = g.Key,
                Assigned = g.Count(),
                Completed = g.Count(b => b.Status == BookingStatus.Completed)
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnerCountRow>> GetDriverUpcomingCountsAsync(DateOnly todayLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.DriverId != null && b.TravelDate >= todayLocal && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Completed)
            .GroupBy(b => b.DriverId!.Value)
            .Select(g => new OwnerCountRow { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnerCountRow>> GetDriverManualAssignmentCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) =>
        await _dbContext.AssignmentHistories
            .Where(a => a.AssignmentType == AssignmentType.Manual && a.AssignedAt >= fromUtc && a.AssignedAt < toUtcExclusive)
            .GroupBy(a => a.DriverId)
            .Select(g => new OwnerCountRow { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnerCountRow>> GetDriverCancelledCountsAsync(DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        // AssignmentHistory is insert-only and never cleared, so it's the only
        // reliable way to attribute a cancelled booking (Cancel() clears
        // Booking.DriverId) to the driver(s) who were assigned before cancellation.
        // Distinct (driver, booking) pairs first, so a booking reassigned back and
        // forth to the same driver is never double-counted.
        var pairs = await _dbContext.AssignmentHistories
            .Where(a => a.Booking!.Status == BookingStatus.Cancelled && a.Booking.CancelledAt >= fromUtc && a.Booking.CancelledAt < toUtcExclusive)
            .Select(a => new { a.DriverId, a.BookingId })
            .Distinct()
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.DriverId)
            .Select(g => new OwnerCountRow { OwnerId = g.Key, Count = g.Count() })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleNameRow>> GetAllVehiclesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Vehicles
            .Select(v => new VehicleNameRow { VehicleId = v.Id, Description = v.Make + " " + v.Model + " - " + v.RegistrationNumber })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VehicleRangeRow>> GetVehicleRangeAggregatesAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.VehicleId != null && b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => b.VehicleId!.Value)
            .Select(g => new VehicleRangeRow
            {
                VehicleId = g.Key,
                Assigned = g.Count(),
                Completed = g.Count(b => b.Status == BookingStatus.Completed),
                Passengers = g.Sum(b => b.PassengerCount)
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnerCountRow>> GetVehicleUpcomingCountsAsync(DateOnly todayLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.VehicleId != null && b.TravelDate >= todayLocal && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Completed)
            .GroupBy(b => b.VehicleId!.Value)
            .Select(g => new OwnerCountRow { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

    public async Task<PassengerAggregate> GetPassengerAggregateAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => 1)
            .Select(g => new PassengerAggregate
            {
                TotalPassengers = g.Sum(b => b.PassengerCount),
                BookingCount = g.Count(),
                MaximumPassengers = g.Max(b => b.PassengerCount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new PassengerAggregate();
    }

    public async Task<IReadOnlyDictionary<string, int>> GetStatusCountsAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Status.ToString(), r => r.Count);
    }

    public async Task<AssignmentBaseAggregate> GetAssignmentBaseAggregateAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status != BookingStatus.Cancelled)
            .GroupBy(b => 1)
            .Select(g => new AssignmentBaseAggregate
            {
                TotalNonCancelled = g.Count(),
                Assigned = g.Count(b => b.AssignmentType != null),
                RequiresManual = g.Count(b => b.RequiresManualAssignment)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return result ?? new AssignmentBaseAggregate();
    }

    public async Task<IReadOnlyList<UnassignedBookingItem>> GetUnassignedBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Where(b => b.RequiresManualAssignment && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new UnassignedBookingItem
            {
                Id = b.Id,
                BookingReference = b.BookingReference,
                BookingDate = b.TravelDate,
                PickupTime = b.PickupTime,
                Route = new BookingRouteSummary
                {
                    DepartureLocation = b.Route!.DepartureLocation,
                    Destination = b.Route.Destination
                },
                CustomerFirstName = b.CustomerFirstName,
                CustomerLastName = b.CustomerLastName,
                PassengerCount = b.PassengerCount,
                Reason = b.ManualAssignmentReason,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

    public Task<int> GetUnassignedCountAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.CountAsync(b => b.RequiresManualAssignment && b.Status != BookingStatus.Cancelled, cancellationToken);

    public async Task<IReadOnlyList<UpcomingOperationItem>> GetUpcomingOperationsAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Include(b => b.Driver).ThenInclude(d => d!.User)
            .Include(b => b.Vehicle)
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
            .Select(b => new UpcomingOperationItem
            {
                Id = b.Id,
                BookingReference = b.BookingReference,
                BookingDate = b.TravelDate,
                PickupTime = b.PickupTime,
                Route = new BookingRouteSummary
                {
                    DepartureLocation = b.Route!.DepartureLocation,
                    Destination = b.Route.Destination
                },
                CustomerFirstName = b.CustomerFirstName,
                CustomerLastName = b.CustomerLastName,
                DriverName = b.Driver == null ? null : (b.Driver.User!.FirstName + " " + b.Driver.User.LastName),
                VehicleDescription = b.Vehicle == null ? null : (b.Vehicle.Make + " " + b.Vehicle.Model + " - " + b.Vehicle.RegistrationNumber),
                Status = b.Status.ToString(),
                RideStatus = b.RideStatus.ToString()
            })
            .ToListAsync(cancellationToken);

    public Task<int> GetCancelledCountInRangeAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.CountAsync(
            b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status == BookingStatus.Cancelled,
            cancellationToken);

    public Task<int> GetTotalCountInRangeAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.CountAsync(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal, cancellationToken);

    public async Task<IReadOnlyList<CancellationsByRouteItem>> GetCancellationsByRouteAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status == BookingStatus.Cancelled)
            .GroupBy(b => new { b.RouteId, b.Route!.DepartureLocation, b.Route.Destination })
            .Select(g => new CancellationsByRouteItem
            {
                RouteId = g.Key.RouteId,
                DepartureLocation = g.Key.DepartureLocation,
                Destination = g.Key.Destination,
                Count = g.Count()
            })
            .OrderByDescending(r => r.Count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CancellationsByDayItem>> GetCancellationsByDayAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status == BookingStatus.Cancelled)
            .GroupBy(b => b.TravelDate)
            .Select(g => new CancellationsByDayItem { Date = g.Key, Count = g.Count() })
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookingReportRow>> GetBookingReportRowsAsync(DateOnly fromLocal, DateOnly toLocal, int maxRows, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Include(b => b.Route)
            .Include(b => b.Driver).ThenInclude(d => d!.User)
            .Include(b => b.Vehicle)
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal)
            .OrderBy(b => b.TravelDate).ThenBy(b => b.PickupTime)
            .Take(maxRows)
            .Select(b => new BookingReportRow
            {
                BookingReference = b.BookingReference,
                Date = b.TravelDate,
                Time = b.PickupTime,
                Route = b.Route!.DepartureLocation + " - " + b.Route.Destination,
                Customer = b.CustomerFirstName + " " + b.CustomerLastName,
                Driver = b.Driver == null ? null : (b.Driver.User!.FirstName + " " + b.Driver.User.LastName),
                Vehicle = b.Vehicle == null ? null : (b.Vehicle.Make + " " + b.Vehicle.Model + " - " + b.Vehicle.RegistrationNumber),
                Passengers = b.PassengerCount,
                Price = b.Price,
                Currency = b.Currency,
                Status = b.Status.ToString(),
                RideStatus = b.RideStatus.ToString()
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CancellationReasonItem>> GetCancellationsByReasonAsync(DateOnly fromLocal, DateOnly toLocal, CancellationToken cancellationToken = default) =>
        await _dbContext.Bookings
            .Where(b => b.TravelDate >= fromLocal && b.TravelDate <= toLocal && b.Status == BookingStatus.Cancelled && b.CancellationReason != null)
            .GroupBy(b => b.CancellationReason!)
            .Select(g => new CancellationReasonItem { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ToListAsync(cancellationToken);
}
