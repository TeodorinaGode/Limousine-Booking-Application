using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainUser = LimousineBooking.Domain.Entities.User;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Application.Drivers;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IPasswordService _passwordService;

    public DriverService(
        IDriverRepository driverRepository,
        IUserRepository userRepository,
        IVehicleRepository vehicleRepository,
        IPasswordService passwordService)
    {
        _driverRepository = driverRepository;
        _userRepository = userRepository;
        _vehicleRepository = vehicleRepository;
        _passwordService = passwordService;
    }

    public async Task<PagedResult<DriverResponse>> SearchAsync(DriverSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _driverRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<DriverResponse>
        {
            Items = items.Select(d => ToResponse(d, d.User!, d.CurrentVehicle)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        return driver is null ? null : ToResponse(driver, driver.User!, driver.CurrentVehicle);
    }

    public async Task<DriverOperationResult> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var phone = request.Phone.Trim();

        if (await _userRepository.HasDuplicateEmailAsync(email, null, cancellationToken))
            return DriverOperationResult.Failure(DriverError.Duplicate, "A user with this email already exists.");

        DomainVehicle? vehicle = null;
        if (request.VehicleId.HasValue)
        {
            var vehicleCheck = await ValidateVehicleForAssignmentAsync(request.VehicleId.Value, excludeDriverId: null, cancellationToken);
            if (vehicleCheck.Error is not null)
                return vehicleCheck.Error;
            vehicle = vehicleCheck.Vehicle;
        }

        DomainUser user;
        DomainDriver driver;
        try
        {
            var passwordHash = _passwordService.Hash(request.Password);
            user = new DomainUser(email, passwordHash, firstName, lastName, UserRole.Driver);
            driver = new DomainDriver(user.Id, phone);
            if (vehicle is not null)
                driver.AssignVehicle(vehicle.Id);
        }
        catch (ArgumentException ex)
        {
            return DriverOperationResult.Failure(DriverError.Validation, ex.Message);
        }

        // A single SaveChangesAsync call after both adds is already atomic —
        // both repositories share this request's scoped DbContext, so EF Core
        // wraps the User and Driver inserts in one database transaction.
        await _userRepository.AddAsync(user, cancellationToken);
        await _driverRepository.AddAsync(driver, cancellationToken);
        await _driverRepository.SaveChangesAsync(cancellationToken);

        return DriverOperationResult.Success(ToResponse(driver, user, vehicle));
    }

    public async Task<DriverOperationResult> UpdateAsync(Guid id, UpdateDriverRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
            return DriverOperationResult.Failure(DriverError.NotFound, "Driver not found.");

        var user = driver.User!;
        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var phone = request.Phone.Trim();

        if (await _userRepository.HasDuplicateEmailAsync(email, user.Id, cancellationToken))
            return DriverOperationResult.Failure(DriverError.Duplicate, "A user with this email already exists.");

        DomainVehicle? vehicle = null;
        if (request.VehicleId.HasValue && request.VehicleId != driver.CurrentVehicleId)
        {
            var vehicleCheck = await ValidateVehicleForAssignmentAsync(request.VehicleId.Value, excludeDriverId: driver.Id, cancellationToken);
            if (vehicleCheck.Error is not null)
                return vehicleCheck.Error;
            vehicle = vehicleCheck.Vehicle;
        }
        else if (request.VehicleId.HasValue)
        {
            vehicle = driver.CurrentVehicle;
        }

        try
        {
            user.UpdateProfile(email, firstName, lastName);
            driver.UpdatePhone(phone);
        }
        catch (ArgumentException ex)
        {
            return DriverOperationResult.Failure(DriverError.Validation, ex.Message);
        }

        if (request.VehicleId.HasValue)
            driver.AssignVehicle(request.VehicleId.Value);
        else
            driver.UnassignVehicle();

        // Deactivating/activating a driver through this endpoint also flips the
        // linked User's login access — see DriverService class docs / README.
        if (request.IsActive)
        {
            driver.Activate();
            user.Activate();
        }
        else
        {
            driver.Deactivate();
            user.Deactivate();
        }

        await _driverRepository.SaveChangesAsync(cancellationToken);

        return DriverOperationResult.Success(ToResponse(driver, user, vehicle));
    }

    public async Task<DriverOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
            return DriverOperationResult.Failure(DriverError.NotFound, "Driver not found.");

        var user = driver.User!;

        if (isActive)
        {
            driver.Activate();
            user.Activate();
        }
        else
        {
            driver.Deactivate();
            user.Deactivate();
        }

        await _driverRepository.SaveChangesAsync(cancellationToken);

        return DriverOperationResult.Success(ToResponse(driver, user, driver.CurrentVehicle));
    }

    public async Task<DriverOperationResult> ResetPasswordAsync(Guid id, ResetDriverPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
            return DriverOperationResult.Failure(DriverError.NotFound, "Driver not found.");

        var user = driver.User!;

        try
        {
            var passwordHash = _passwordService.Hash(request.NewPassword);
            user.SetPasswordHash(passwordHash);
        }
        catch (ArgumentException ex)
        {
            return DriverOperationResult.Failure(DriverError.Validation, ex.Message);
        }

        await _driverRepository.SaveChangesAsync(cancellationToken);

        return DriverOperationResult.Success(ToResponse(driver, user, driver.CurrentVehicle));
    }

    private async Task<(DomainVehicle? Vehicle, DriverOperationResult? Error)> ValidateVehicleForAssignmentAsync(
        Guid vehicleId, Guid? excludeDriverId, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null)
            return (null, DriverOperationResult.Failure(DriverError.Validation, "Vehicle not found."));
        if (!vehicle.IsActive)
            return (null, DriverOperationResult.Failure(DriverError.Validation, "Only active vehicles can be assigned to a driver."));
        if (await _driverRepository.IsVehicleAssignedToAnotherDriverAsync(vehicleId, excludeDriverId, cancellationToken))
            return (null, DriverOperationResult.Failure(DriverError.Duplicate, "This vehicle is already assigned to another driver."));

        return (vehicle, null);
    }

    private static DriverResponse ToResponse(DomainDriver driver, DomainUser user, DomainVehicle? vehicle) => new()
    {
        Id = driver.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = driver.Phone,
        IsActive = driver.IsActive,
        IsAvailable = driver.IsAvailable,
        Vehicle = vehicle is null
            ? null
            : new DriverVehicleSummary
            {
                Id = vehicle.Id,
                RegistrationNumber = vehicle.RegistrationNumber,
                Make = vehicle.Make,
                Model = vehicle.Model
            },
        CreatedAt = driver.CreatedAt,
        UpdatedAt = driver.UpdatedAt
    };
}
