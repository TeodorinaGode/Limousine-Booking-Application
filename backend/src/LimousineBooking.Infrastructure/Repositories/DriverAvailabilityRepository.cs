using LimousineBooking.Application.Interfaces;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainAvailability = LimousineBooking.Domain.Entities.DriverAvailability;

namespace LimousineBooking.Infrastructure.Repositories;

public class DriverAvailabilityRepository : IDriverAvailabilityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DriverAvailabilityRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainAvailability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DriverAvailabilities.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DomainAvailability>> GetByDriverAsync(Guid driverId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DriverAvailabilities.Where(a => a.DriverId == driverId);

        if (from.HasValue)
            query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Date <= to.Value);

        return await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlapAsync(Guid driverId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        // Half-open interval overlap, translated directly to SQL (can't call
        // the domain's Overlaps() helper here — EF Core can't translate
        // arbitrary instance methods into SQL).
        var query = _dbContext.DriverAvailabilities.Where(a =>
            a.DriverId == driverId &&
            a.Date == date &&
            a.StartTime < endTime &&
            startTime < a.EndTime);

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(DomainAvailability availability, CancellationToken cancellationToken = default)
    {
        _dbContext.DriverAvailabilities.Add(availability);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DomainAvailability availability, CancellationToken cancellationToken = default)
    {
        _dbContext.DriverAvailabilities.Remove(availability);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
