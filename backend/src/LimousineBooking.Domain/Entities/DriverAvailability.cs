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
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time.", nameof(endTime));

        DriverId = driverId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = isAvailable;
        Notes = notes;
    }
}
