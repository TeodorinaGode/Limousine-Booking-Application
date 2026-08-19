namespace LimousineBooking.Application.Bookings;

/// <summary>Minimal booking projection for the admin list table — only the fields the table displays (section 41: detailed data belongs to the single-booking endpoint, not the list).</summary>
public class AdminBookingListItemResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public BookingRouteSummary Route { get; set; } = new();
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public int PassengerCount { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? VehicleDescription { get; set; }

    /// <summary>"Automatic", "Manual", or "Unassigned".</summary>
    public string Assignment { get; set; } = string.Empty;
}
