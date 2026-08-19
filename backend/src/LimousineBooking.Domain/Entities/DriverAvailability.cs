using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

public class DriverAvailability : AuditableEntity
{
    public Guid DriverId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? Notes { get; private set; }

    public Driver? Driver { get; private set; }

    private DriverAvailability()
    {
    }

    public DriverAvailability(Guid driverId, DateOnly date, TimeOnly startTime, TimeOnly endTime, bool isAvailable, string? notes = null)
    {
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        Validate(startTime, endTime);

        DriverId = driverId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = isAvailable;
        Notes = notes;
    }

    public void Update(DateOnly date, TimeOnly startTime, TimeOnly endTime, bool isAvailable, string? notes)
    {
        Validate(startTime, endTime);

        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = isAvailable;
        Notes = notes;
    }

    /// <summary>Half-open interval overlap: this period and another starting at <paramref name="otherStart"/> ending at <paramref name="otherEnd"/> overlap.</summary>
    public bool Overlaps(TimeOnly otherStart, TimeOnly otherEnd) => StartTime < otherEnd && otherStart < EndTime;

    private static void Validate(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time.", nameof(endTime));
    }
}
