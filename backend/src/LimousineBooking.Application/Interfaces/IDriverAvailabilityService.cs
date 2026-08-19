using LimousineBooking.Application.Availability;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Self-service driver availability (current-status toggle + schedule CRUD)
/// and the read-only admin schedule view. Every method takes an explicit
/// <c>driverId</c> — callers must resolve it from a trusted source
/// (ICurrentUserService for self-service, the admin route parameter for the
/// admin view), never from a request body.
/// </summary>
public interface IDriverAvailabilityService
{
    /// <summary>Null if the driver does not exist.</summary>
    Task<DriverScheduleResponse?> GetScheduleAsync(Guid driverId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<AvailabilityOperationResult> CreateAsync(Guid driverId, CreateAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<AvailabilityOperationResult> UpdateAsync(Guid driverId, Guid availabilityId, UpdateAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<AvailabilityOperationResult> DeleteAsync(Guid driverId, Guid availabilityId, CancellationToken cancellationToken = default);

    /// <summary>Null if the driver does not exist; otherwise the resulting IsAvailable value.</summary>
    Task<bool?> SetCurrentAvailabilityAsync(Guid driverId, bool isAvailable, CancellationToken cancellationToken = default);
}
