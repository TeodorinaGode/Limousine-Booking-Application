namespace LimousineBooking.Application.Bookings;

public class BookingResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public BookingRouteSummary Route { get; set; } = new();

    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string? Notes { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}

/// <summary>Route detail nested in <see cref="BookingResponse"/> — just enough to confirm the trip, not the full route record.</summary>
public class BookingRouteSummary
{
    public string DepartureLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}
