using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only vehicle management: list/search, create, edit, and
/// activate/deactivate. Deactivating a vehicle never deletes it — historical
/// booking references must remain valid.
/// </summary>
[ApiController]
[Route("api/admin/vehicles")]
[Authorize(Roles = "Administrator")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>List/search vehicles with optional filtering, sorting, and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<VehicleResponse>>> GetAll([FromQuery] VehicleSearchQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _vehicleService.SearchAsync(query, cancellationToken));
    }

    /// <summary>Get a single vehicle by id.</summary>
    /// <response code="404">No vehicle exists with the given id.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, cancellationToken);
        return vehicle is null ? NotFound(new { message = "Vehicle not found." }) : Ok(vehicle);
    }

    /// <summary>Create a vehicle. Active by default. Registration number must be unique.</summary>
    /// <response code="409">A vehicle with this registration number already exists.</response>
    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Vehicle!.Id }, result.Vehicle)
            : MapError(result);
    }

    /// <summary>Full update, including active status.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Vehicle) : MapError(result);
    }

    /// <summary>Convenience endpoint to activate a vehicle without resending the full form.</summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult<VehicleResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.SetActiveAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Vehicle) : MapError(result);
    }

    /// <summary>Convenience endpoint to deactivate a vehicle. Never deletes it.</summary>
    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<VehicleResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.SetActiveAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Vehicle) : MapError(result);
    }

    private ActionResult MapError(VehicleOperationResult result) => result.Error switch
    {
        VehicleError.NotFound => NotFound(new { message = result.ErrorMessage }),
        VehicleError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        VehicleError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
