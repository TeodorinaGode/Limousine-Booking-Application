using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Application.Vehicles;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<PagedResult<VehicleResponse>> SearchAsync(VehicleSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _vehicleRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<VehicleResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        return vehicle is null ? null : ToResponse(vehicle);
    }

    public async Task<VehicleOperationResult> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var registrationNumber = NormalizeRegistrationNumber(request.RegistrationNumber);
        var make = request.Make.Trim();
        var model = request.Model.Trim();
        var vehicleType = request.VehicleType.Trim();

        if (await _vehicleRepository.HasDuplicateRegistrationAsync(registrationNumber, null, cancellationToken))
            return VehicleOperationResult.Failure(VehicleError.Duplicate, "A vehicle with this registration number already exists.");

        DomainVehicle vehicle;
        try
        {
            vehicle = new DomainVehicle(registrationNumber, make, model, vehicleType, request.PassengerCapacity, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return VehicleOperationResult.Failure(VehicleError.Validation, ex.Message);
        }

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _vehicleRepository.SaveChangesAsync(cancellationToken);

        return VehicleOperationResult.Success(ToResponse(vehicle));
    }

    public async Task<VehicleOperationResult> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
            return VehicleOperationResult.Failure(VehicleError.NotFound, "Vehicle not found.");

        var registrationNumber = NormalizeRegistrationNumber(request.RegistrationNumber);
        var make = request.Make.Trim();
        var model = request.Model.Trim();
        var vehicleType = request.VehicleType.Trim();

        if (await _vehicleRepository.HasDuplicateRegistrationAsync(registrationNumber, id, cancellationToken))
            return VehicleOperationResult.Failure(VehicleError.Duplicate, "A vehicle with this registration number already exists.");

        try
        {
            vehicle.Update(registrationNumber, make, model, vehicleType, request.PassengerCapacity, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return VehicleOperationResult.Failure(VehicleError.Validation, ex.Message);
        }

        if (request.IsActive)
            vehicle.Activate();
        else
            vehicle.Deactivate();

        await _vehicleRepository.SaveChangesAsync(cancellationToken);

        return VehicleOperationResult.Success(ToResponse(vehicle));
    }

    public async Task<VehicleOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
            return VehicleOperationResult.Failure(VehicleError.NotFound, "Vehicle not found.");

        if (isActive)
            vehicle.Activate();
        else
            vehicle.Deactivate();

        await _vehicleRepository.SaveChangesAsync(cancellationToken);

        return VehicleOperationResult.Success(ToResponse(vehicle));
    }

    /// <summary>Trims, collapses internal whitespace runs to a single space, and uppercases.</summary>
    private static string NormalizeRegistrationNumber(string value)
    {
        var tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tokens).ToUpperInvariant();
    }

    private static VehicleResponse ToResponse(DomainVehicle vehicle) => new()
    {
        Id = vehicle.Id,
        RegistrationNumber = vehicle.RegistrationNumber,
        Make = vehicle.Make,
        Model = vehicle.Model,
        VehicleType = vehicle.VehicleType,
        PassengerCapacity = vehicle.PassengerCapacity,
        IsActive = vehicle.IsActive,
        Notes = vehicle.Notes,
        CreatedAt = vehicle.CreatedAt,
        UpdatedAt = vehicle.UpdatedAt
    };
}
