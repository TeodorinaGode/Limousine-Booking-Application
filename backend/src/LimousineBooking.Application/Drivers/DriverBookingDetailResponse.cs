using LimousineBooking.Application.Bookings;

namespace LimousineBooking.Application.Drivers;

/// <summary>
/// Full trip detail for GET /api/driver/bookings/{id} — customer contact info a
/// driver legitimately needs (name, phone, notes) but never the customer's email,
/// mirroring the field set already sent in the driver-assignment notification.
/// </summary>
public class DriverBookingDetailResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;

    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public BookingRouteSummary Route { get; set; } = new();
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public TimeOnly EstimatedEndTime { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string? Notes { get; set; }

    public string Status { get; set; } = string.Empty;
    public string RideStatus { get; set; } = string.Empty;

    public IReadOnlyList<RideStatusHistoryItem> RideStatusHistory { get; set; } = Array.Empty<RideStatusHistoryItem>();
}

public class RideStatusHistoryItem
{
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
