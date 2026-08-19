using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Routes;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Infrastructure.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RouteRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainRoute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Routes.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<DomainRoute> Items, int TotalCount)> SearchAsync(RouteSearchQuery query, CancellationToken cancellationToken = default)
    {
        var routes = _dbContext.Routes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            routes = routes.Where(r =>
                EF.Functions.ILike(r.DepartureLocation, pattern) ||
                EF.Functions.ILike(r.Destination, pattern));
        }

        if (query.IsActive.HasValue)
            routes = routes.Where(r => r.IsActive == query.IsActive.Value);

        var totalCount = await routes.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        routes = query.SortBy?.ToLowerInvariant() switch
        {
            "destination" => descending ? routes.OrderByDescending(r => r.Destination) : routes.OrderBy(r => r.Destination),
            "duration" => descending ? routes.OrderByDescending(r => r.EstimatedDurationMinutes) : routes.OrderBy(r => r.EstimatedDurationMinutes),
            "price" => descending ? routes.OrderByDescending(r => r.Price) : routes.OrderBy(r => r.Price),
            "status" => descending ? routes.OrderByDescending(r => r.IsActive) : routes.OrderBy(r => r.IsActive),
            "createdat" => descending ? routes.OrderByDescending(r => r.CreatedAt) : routes.OrderBy(r => r.CreatedAt),
            // "departure" and any unrecognized value fall back to the default —
            // sortBy is never used to build raw SQL, only to pick a known column.
            _ => descending ? routes.OrderByDescending(r => r.DepartureLocation) : routes.OrderBy(r => r.DepartureLocation)
        };

        var items = await routes
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> HasActiveDuplicateAsync(string departureLocation, string destination, Guid? excludeRouteId, CancellationToken cancellationToken = default)
    {
        var normalizedDeparture = departureLocation.Trim();
        var normalizedDestination = destination.Trim();

        var query = _dbContext.Routes.Where(r =>
            r.IsActive &&
            EF.Functions.ILike(r.DepartureLocation, normalizedDeparture) &&
            EF.Functions.ILike(r.Destination, normalizedDestination));

        if (excludeRouteId.HasValue)
            query = query.Where(r => r.Id != excludeRouteId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(DomainRoute route, CancellationToken cancellationToken = default)
    {
        _dbContext.Routes.Add(route);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
