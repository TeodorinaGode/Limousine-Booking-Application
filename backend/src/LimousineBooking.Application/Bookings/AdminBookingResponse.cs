namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Booking shape prepared for the future Admin Booking Management UI (not yet
/// built — no controller returns this shape yet). Unlike the public
/// <see cref="BookingResponse"/>, this includes assignment internals an
/// administrator needs and a customer must never see.
/// </summary>
public class AdminBookingResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;

    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public BookingRouteSummary Route { get; set; } = new();
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? DriverName { get; set; }
    public string? VehicleDescription { get; set; }

    /// <summary>"Automatic", "Manual", or null if not yet assigned.</summary>
    public string? AssignmentType { get; set; }

    public bool RequiresManualAssignment { get; set; }
    public string? ManualAssignmentReason { get; set; }
}
