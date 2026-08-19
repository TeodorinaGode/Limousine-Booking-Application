using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Vehicles;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public VehicleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Vehicles.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<DomainVehicle> Items, int TotalCount)> SearchAsync(VehicleSearchQuery query, CancellationToken cancellationToken = default)
    {
        var vehicles = _dbContext.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            vehicles = vehicles.Where(v =>
                EF.Functions.ILike(v.RegistrationNumber, pattern) ||
                EF.Functions.ILike(v.Make, pattern) ||
                EF.Functions.ILike(v.Model, pattern) ||
                EF.Functions.ILike(v.VehicleType, pattern));
        }

        if (query.IsActive.HasValue)
            vehicles = vehicles.Where(v => v.IsActive == query.IsActive.Value);

        if (query.MinCapacity.HasValue)
            vehicles = vehicles.Where(v => v.PassengerCapacity >= query.MinCapacity.Value);

        var totalCount = await vehicles.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        vehicles = query.SortBy?.ToLowerInvariant() switch
        {
            "make" => descending ? vehicles.OrderByDescending(v => v.Make) : vehicles.OrderBy(v => v.Make),
            "model" => descending ? vehicles.OrderByDescending(v => v.Model) : vehicles.OrderBy(v => v.Model),
            "passengercapacity" => descending ? vehicles.OrderByDescending(v => v.PassengerCapacity) : vehicles.OrderBy(v => v.PassengerCapacity),
            "createdat" => descending ? vehicles.OrderByDescending(v => v.CreatedAt) : vehicles.OrderBy(v => v.CreatedAt),
            // "registrationNumber" and any unrecognized value fall back to the
            // default — sortBy is never used to build raw SQL, only to pick a known column.
            _ => descending ? vehicles.OrderByDescending(v => v.RegistrationNumber) : vehicles.OrderBy(v => v.RegistrationNumber)
        };

        var items = await vehicles
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> HasDuplicateRegistrationAsync(string registrationNumber, Guid? excludeVehicleId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vehicles.Where(v => EF.Functions.ILike(v.RegistrationNumber, registrationNumber));

        if (excludeVehicleId.HasValue)
            query = query.Where(v => v.Id != excludeVehicleId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(DomainVehicle vehicle, CancellationToken cancellationToken = default)
    {
        _dbContext.Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
