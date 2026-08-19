using LimousineBooking.Application.Routes;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Route> Items, int TotalCount)> SearchAsync(RouteSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>All active routes, for the public routes listing. No pagination — this is a small, public reference list.</summary>
    Task<IReadOnlyList<Route>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True if an *active* route already exists for this departure/destination pair
    /// (trimmed, case-insensitive), excluding <paramref name="excludeRouteId"/> if given.
    /// </summary>
    Task<bool> HasActiveDuplicateAsync(string departureLocation, string destination, Guid? excludeRouteId, CancellationToken cancellationToken = default);

    Task AddAsync(Route route, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
