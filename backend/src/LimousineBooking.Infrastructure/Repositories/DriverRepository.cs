using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DriverRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Drivers.SingleOrDefaultAsync(d => d.UserId == userId, cancellationToken);

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Drivers
            .Include(d => d.User)
            .Include(d => d.CurrentVehicle)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Driver> Items, int TotalCount)> SearchAsync(DriverSearchQuery query, CancellationToken cancellationToken = default)
    {
        var drivers = _dbContext.Drivers
            .Include(d => d.User)
            .Include(d => d.CurrentVehicle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            drivers = drivers.Where(d =>
                EF.Functions.ILike(d.User!.FirstName, pattern) ||
                EF.Functions.ILike(d.User!.LastName, pattern) ||
                EF.Functions.ILike(d.User!.Email, pattern) ||
                EF.Functions.ILike(d.Phone, pattern));
        }

        if (query.IsActive.HasValue)
            drivers = drivers.Where(d => d.IsActive == query.IsActive.Value);

        if (query.IsAvailable.HasValue)
            drivers = drivers.Where(d => d.IsAvailable == query.IsAvailable.Value);

        if (query.HasVehicle.HasValue)
        {
            drivers = query.HasVehicle.Value
                ? drivers.Where(d => d.CurrentVehicleId != null)
                : drivers.Where(d => d.CurrentVehicleId == null);
        }

        var totalCount = await drivers.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        drivers = query.SortBy?.ToLowerInvariant() switch
        {
            "lastname" => descending ? drivers.OrderByDescending(d => d.User!.LastName) : drivers.OrderBy(d => d.User!.LastName),
            "email" => descending ? drivers.OrderByDescending(d => d.User!.Email) : drivers.OrderBy(d => d.User!.Email),
            "createdat" => descending ? drivers.OrderByDescending(d => d.CreatedAt) : drivers.OrderBy(d => d.CreatedAt),
            // "firstName" and any unrecognized value fall back to the default —
            // sortBy is never used to build raw SQL, only to pick a known column.
            _ => descending ? drivers.OrderByDescending(d => d.User!.FirstName) : drivers.OrderBy(d => d.User!.FirstName)
        };

        var items = await drivers
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> IsVehicleAssignedToAnotherDriverAsync(Guid vehicleId, Guid? excludeDriverId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Drivers.Where(d => d.CurrentVehicleId == vehicleId);

        if (excludeDriverId.HasValue)
            query = query.Where(d => d.Id != excludeDriverId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        _dbContext.Drivers.Add(driver);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
