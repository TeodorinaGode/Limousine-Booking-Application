using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/routes")]
[Authorize(Roles = "Administrator")]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routeService;

    public RoutesController(IRouteService routeService)
    {
        _routeService = routeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RouteResponse>>> GetAll([FromQuery] RouteSearchQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _routeService.SearchAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var route = await _routeService.GetByIdAsync(id, cancellationToken);
        return route is null ? NotFound(new { message = "Route not found." }) : Ok(route);
    }

    [HttpPost]
    public async Task<ActionResult<RouteResponse>> Create([FromBody] CreateRouteRequest request, CancellationToken cancellationToken)
    {
        var result = await _routeService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Route!.Id }, result.Route)
            : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RouteResponse>> Update(Guid id, [FromBody] UpdateRouteRequest request, CancellationToken cancellationToken)
    {
        var result = await _routeService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Route) : MapError(result);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<RouteResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _routeService.SetActiveAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Route) : MapError(result);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<RouteResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _routeService.SetActiveAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Route) : MapError(result);
    }

    private ActionResult MapError(RouteOperationResult result) => result.Error switch
    {
        RouteError.NotFound => NotFound(new { message = result.ErrorMessage }),
        RouteError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        RouteError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
