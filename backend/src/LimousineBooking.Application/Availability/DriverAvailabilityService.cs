using LimousineBooking.Application.Interfaces;
using DomainAvailability = LimousineBooking.Domain.Entities.DriverAvailability;

namespace LimousineBooking.Application.Availability;

public class DriverAvailabilityService : IDriverAvailabilityService
{
    private readonly IDriverAvailabilityRepository _availabilityRepository;
    private readonly IDriverRepository _driverRepository;

    public DriverAvailabilityService(IDriverAvailabilityRepository availabilityRepository, IDriverRepository driverRepository)
    {
        _availabilityRepository = availabilityRepository;
        _driverRepository = driverRepository;
    }

    public async Task<DriverScheduleResponse?> GetScheduleAsync(Guid driverId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver is null)
            return null;

        var records = await _availabilityRepository.GetByDriverAsync(driverId, from, to, cancellationToken);

        return new DriverScheduleResponse
        {
            IsCurrentlyAvailable = driver.IsAvailable,
            Schedule = records.Select(ToResponse).ToList()
        };
    }

    public async Task<AvailabilityOperationResult> CreateAsync(Guid driverId, CreateAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver is null)
            return AvailabilityOperationResult.Failure(AvailabilityError.NotFound, "Driver not found.");
        if (!driver.IsActive)
            return AvailabilityOperationResult.Failure(AvailabilityError.Validation, "Inactive drivers cannot create availability records.");

        if (await _availabilityRepository.HasOverlapAsync(driverId, request.Date, request.StartTime, request.EndTime, null, cancellationToken))
            return AvailabilityOperationResult.Failure(AvailabilityError.Conflict, "The driver already has an overlapping availability period for this date.");

        DomainAvailability availability;
        try
        {
            availability = new DomainAvailability(driverId, request.Date, request.StartTime, request.EndTime, request.IsAvailable, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return AvailabilityOperationResult.Failure(AvailabilityError.Validation, ex.Message);
        }

        await _availabilityRepository.AddAsync(availability, cancellationToken);
        await _availabilityRepository.SaveChangesAsync(cancellationToken);

        return AvailabilityOperationResult.Success(ToResponse(availability));
    }

    public async Task<AvailabilityOperationResult> UpdateAsync(Guid driverId, Guid availabilityId, UpdateAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var availability = await _availabilityRepository.GetByIdAsync(availabilityId, cancellationToken);
        // Ownership check folded into "not found" so we never confirm another
        // driver's record even exists.
        if (availability is null || availability.DriverId != driverId)
            return AvailabilityOperationResult.Failure(AvailabilityError.NotFound, "Availability record not found.");

        if (await _availabilityRepository.HasOverlapAsync(driverId, request.Date, request.StartTime, request.EndTime, availabilityId, cancellationToken))
            return AvailabilityOperationResult.Failure(AvailabilityError.Conflict, "The driver already has an overlapping availability period for this date.");

        try
        {
            availability.Update(request.Date, request.StartTime, request.EndTime, request.IsAvailable, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return AvailabilityOperationResult.Failure(AvailabilityError.Validation, ex.Message);
        }

        await _availabilityRepository.SaveChangesAsync(cancellationToken);

        return AvailabilityOperationResult.Success(ToResponse(availability));
    }

    public async Task<AvailabilityOperationResult> DeleteAsync(Guid driverId, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        var availability = await _availabilityRepository.GetByIdAsync(availabilityId, cancellationToken);
        if (availability is null || availability.DriverId != driverId)
            return AvailabilityOperationResult.Failure(AvailabilityError.NotFound, "Availability record not found.");

        await _availabilityRepository.DeleteAsync(availability, cancellationToken);
        await _availabilityRepository.SaveChangesAsync(cancellationToken);

        return AvailabilityOperationResult.Success(ToResponse(availability));
    }

    public async Task<bool?> SetCurrentAvailabilityAsync(Guid driverId, bool isAvailable, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver is null)
            return null;

        if (isAvailable)
            driver.SetAvailable();
        else
            driver.SetUnavailable();

        await _driverRepository.SaveChangesAsync(cancellationToken);

        return driver.IsAvailable;
    }

    private static AvailabilityResponse ToResponse(DomainAvailability availability) => new()
    {
        Id = availability.Id,
        DriverId = availability.DriverId,
        Date = availability.Date,
        StartTime = availability.StartTime,
        EndTime = availability.EndTime,
        IsAvailable = availability.IsAvailable,
        Notes = availability.Notes,
        CreatedAt = availability.CreatedAt,
        UpdatedAt = availability.UpdatedAt
    };
}
