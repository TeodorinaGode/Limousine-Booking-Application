using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Availability;

public class AvailabilityEvaluationService : IAvailabilityEvaluationService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IDriverAvailabilityRepository _availabilityRepository;

    public AvailabilityEvaluationService(IDriverRepository driverRepository, IDriverAvailabilityRepository availabilityRepository)
    {
        _driverRepository = driverRepository;
        _availabilityRepository = availabilityRepository;
    }

    public async Task<bool> IsDriverAvailableAsync(Guid driverId, DateOnly date, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver is null || !driver.IsActive)
            return false;

        var dayRecords = await _availabilityRepository.GetByDriverAsync(driverId, date, date, cancellationToken);

        var blockedByUnavailablePeriod = dayRecords
            .Where(r => !r.IsAvailable)
            .Any(r => r.Overlaps(startTime, endTime));
        return !blockedByUnavailablePeriod;
    }
}
