namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Reusable eligibility check for a future automatic-assignment feature.
/// Deliberately does not check booking conflicts yet — that step can be
/// added later without changing this service's contract.
/// </summary>
public interface IAvailabilityEvaluationService
{
    /// <summary>
    /// True if the driver is active, has an available schedule record fully
    /// containing [startTime, endTime) on <paramref name="date"/>, and has no
    /// unavailable record overlapping that window.
    /// </summary>
    Task<bool> IsDriverAvailableAsync(Guid driverId, DateOnly date, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default);
}
