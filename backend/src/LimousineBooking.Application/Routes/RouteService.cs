using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Application.Routes;

public class RouteService : IRouteService
{
    private readonly IRouteRepository _routeRepository;

    public RouteService(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<PagedResult<RouteResponse>> SearchAsync(RouteSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _routeRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<RouteResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RouteResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(id, cancellationToken);
        return route is null ? null : ToResponse(route);
    }

    public async Task<RouteOperationResult> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken = default)
    {
        var departure = request.DepartureLocation.Trim();
        var destination = request.Destination.Trim();
        var currency = request.Currency.Trim().ToUpperInvariant();

        if (await _routeRepository.HasActiveDuplicateAsync(departure, destination, null, cancellationToken))
            return RouteOperationResult.Failure(RouteError.Duplicate, "An active route with this departure and destination already exists.");

        DomainRoute route;
        try
        {
            route = new DomainRoute(departure, destination, request.EstimatedDurationMinutes, request.Price, currency);
        }
        catch (ArgumentException ex)
        {
            return RouteOperationResult.Failure(RouteError.Validation, ex.Message);
        }

        await _routeRepository.AddAsync(route, cancellationToken);
        await _routeRepository.SaveChangesAsync(cancellationToken);

        return RouteOperationResult.Success(ToResponse(route));
    }

    public async Task<RouteOperationResult> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(id, cancellationToken);
        if (route is null)
            return RouteOperationResult.Failure(RouteError.NotFound, "Route not found.");

        var departure = request.DepartureLocation.Trim();
        var destination = request.Destination.Trim();
        var currency = request.Currency.Trim().ToUpperInvariant();

        if (request.IsActive && await _routeRepository.HasActiveDuplicateAsync(departure, destination, id, cancellationToken))
            return RouteOperationResult.Failure(RouteError.Duplicate, "An active route with this departure and destination already exists.");

        try
        {
            route.Update(departure, destination, request.EstimatedDurationMinutes, request.Price, currency);
        }
        catch (ArgumentException ex)
        {
            return RouteOperationResult.Failure(RouteError.Validation, ex.Message);
        }

        if (request.IsActive)
            route.Activate();
        else
            route.Deactivate();

        await _routeRepository.SaveChangesAsync(cancellationToken);

        return RouteOperationResult.Success(ToResponse(route));
    }

    public async Task<RouteOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(id, cancellationToken);
        if (route is null)
            return RouteOperationResult.Failure(RouteError.NotFound, "Route not found.");

        if (isActive)
        {
            // Re-activating a previously deactivated route can reintroduce a duplicate.
            if (await _routeRepository.HasActiveDuplicateAsync(route.DepartureLocation, route.Destination, id, cancellationToken))
                return RouteOperationResult.Failure(RouteError.Duplicate, "An active route with this departure and destination already exists.");

            route.Activate();
        }
        else
        {
            route.Deactivate();
        }

        await _routeRepository.SaveChangesAsync(cancellationToken);

        return RouteOperationResult.Success(ToResponse(route));
    }

    private static RouteResponse ToResponse(DomainRoute route) => new()
    {
        Id = route.Id,
        DepartureLocation = route.DepartureLocation,
        Destination = route.Destination,
        EstimatedDurationMinutes = route.EstimatedDurationMinutes,
        Price = route.Price,
        Currency = route.Currency,
        IsActive = route.IsActive,
        CreatedAt = route.CreatedAt,
        UpdatedAt = route.UpdatedAt
    };
}
