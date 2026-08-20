namespace LimousineBooking.Application.Reports;

// Small, unshaped aggregation results passed from IReportRepository to
// ReportService — the service adds cross-cutting math (percentages, rates,
// zero-filling for drivers/vehicles with no bookings) that a single grouped
// SQL query can't express, so these stay one step short of the final response DTOs.

public class BookingCreatedAggregate
{
    public int Total { get; set; }
    public int Confirmed { get; set; }
    public int Pending { get; set; }
    public decimal GrossRevenue { get; set; }
}

public class CompletedAggregate
{
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class AssignmentCountAggregate
{
    public int Manual { get; set; }
    public int Automatic { get; set; }
}

public class PassengerAggregate
{
    public int TotalPassengers { get; set; }
    public int BookingCount { get; set; }
    public int MaximumPassengers { get; set; }
}

public class PaymentAggregate
{
    public int Total { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int Refunded { get; set; }
    public decimal PaidRevenue { get; set; }
    public decimal RefundedAmount { get; set; }
}

public class AssignmentBaseAggregate
{
    public int TotalNonCancelled { get; set; }
    public int Assigned { get; set; }
    public int RequiresManual { get; set; }
}

public class DriverRangeRow
{
    public Guid DriverId { get; set; }
    public int Assigned { get; set; }
    public int Completed { get; set; }
}

public class VehicleRangeRow
{
    public Guid VehicleId { get; set; }
    public int Assigned { get; set; }
    public int Completed { get; set; }
    public int Passengers { get; set; }
}

/// <summary>A count keyed by an owner id — reused for driver/vehicle Upcoming counts and driver Manual/Cancelled counts.</summary>
public class OwnerCountRow
{
    public Guid OwnerId { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Every driver's id + display name, independent of the admin driver-search
/// endpoint's pagination (which caps at 100) — a report must include every
/// driver, including ones with zero bookings in range (section 59).
/// </summary>
public class DriverNameRow
{
    public Guid DriverId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VehicleNameRow
{
    public Guid VehicleId { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>One row of the bookings CSV export (section 32/54) — capped server-side, never streamed from an already-loaded paginated page.</summary>
public class BookingReportRow
{
    public string BookingReference { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string? Driver { get; set; }
    public string? Vehicle { get; set; }
    public int Passengers { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RideStatus { get; set; } = string.Empty;
}
