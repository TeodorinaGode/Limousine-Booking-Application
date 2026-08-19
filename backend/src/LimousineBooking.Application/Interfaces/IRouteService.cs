using LimousineBooking.Application.Common;
using LimousineBooking.Application.Routes;

namespace LimousineBooking.Application.Interfaces;

public interface IRouteService
{
    Task<PagedResult<RouteResponse>> SearchAsync(RouteSearchQuery query, CancellationToken cancellationToken = default);

    Task<RouteResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RouteOperationResult> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken = default);

    Task<RouteOperationResult> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken cancellationToken = default);

    Task<RouteOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
