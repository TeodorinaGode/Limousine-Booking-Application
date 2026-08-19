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

    public Task<Booking?> GetByIdWithRouteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Bookings.Include(b => b.Route).SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

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
}
