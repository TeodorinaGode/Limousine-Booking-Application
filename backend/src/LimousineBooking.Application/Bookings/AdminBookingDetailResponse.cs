namespace LimousineBooking.Application.Bookings;

/// <summary>Full booking detail for GET /api/admin/bookings/{id} — includes assignment internals and customer PII a customer must never see.</summary>
public class AdminBookingDetailResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;

    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public Guid RouteId { get; set; }
    public BookingRouteSummary Route { get; set; } = new();
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    /// <summary>Derived from PickupTime + Route.EstimatedDurationMinutes — never stored.</summary>
    public TimeOnly EstimatedEndTime { get; set; }

    public string PickupAddress { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string? Notes { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    /// <summary>Trip progress (Upcoming/OnTheWay/PassengerPickedUp/Completed/Cancelled) — view-only here; only the driver's own endpoints can change it.</summary>
    public string RideStatus { get; set; } = string.Empty;
    public IReadOnlyList<RideStatusHistoryEntry> RideStatusHistory { get; set; } = Array.Empty<RideStatusHistoryEntry>();

    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleDescription { get; set; }

    /// <summary>"Automatic", "Manual", or null if not yet assigned.</summary>
    public string? AssignmentType { get; set; }

    public bool RequiresManualAssignment { get; set; }
    public string? ManualAssignmentReason { get; set; }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByEmail { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<AssignmentHistoryItem> AssignmentHistory { get; set; } = Array.Empty<AssignmentHistoryItem>();
}

public class AssignmentHistoryItem
{
    public string DriverName { get; set; } = string.Empty;
    public string VehicleDescription { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public string? AssignedByEmail { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class RideStatusHistoryEntry
{
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
