namespace LimousineBooking.Application.Availability;

public class UpdateAvailabilityRequest
{
    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsAvailable { get; set; }

    public string? Notes { get; set; }
}
