namespace LimousineBooking.Application.Availability;

/// <summary>
/// Bundles the driver's real-time availability flag with their schedule —
/// both are needed together by the availability page on first load, and the
/// spec doesn't define a separate "get current status" endpoint.
/// </summary>
public class DriverScheduleResponse
{
    public bool IsCurrentlyAvailable { get; set; }
    public IReadOnlyList<AvailabilityResponse> Schedule { get; set; } = Array.Empty<AvailabilityResponse>();
}
