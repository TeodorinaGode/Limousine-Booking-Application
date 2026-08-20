using LimousineBooking.Application.Bookings;

namespace LimousineBooking.Application.Drivers;

/// <summary>One trip on the driver's own schedule/dashboard — trimmed to what a driver needs at a glance.</summary>
public class DriverBookingListItemResponse
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public BookingRouteSummary Route { get; set; } = new();
    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RideStatus { get; set; } = string.Empty;
}
