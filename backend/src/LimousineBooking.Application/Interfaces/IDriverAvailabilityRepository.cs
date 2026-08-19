using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IDriverAvailabilityRepository
{
    Task<DriverAvailability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ordered by date then start time. <paramref name="from"/>/<paramref name="to"/> are inclusive.</summary>
    Task<IReadOnlyList<DriverAvailability>> GetByDriverAsync(Guid driverId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the driver already has an availability record for this date whose
    /// time range overlaps [startTime, endTime), excluding <paramref name="excludeId"/> if given.
    /// Checked regardless of IsAvailable — two records covering the same slot is
    /// ambiguous data even if one is "available" and the other "unavailable".
    /// </summary>
    Task<bool> HasOverlapAsync(Guid driverId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludeId, CancellationToken cancellationToken = default);

    Task AddAsync(DriverAvailability availability, CancellationToken cancellationToken = default);

    Task DeleteAsync(DriverAvailability availability, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
