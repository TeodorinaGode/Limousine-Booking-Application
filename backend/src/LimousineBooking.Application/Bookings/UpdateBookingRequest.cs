using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Administrator edit of a booking's trip/customer details. Deliberately has no
/// Status, Price, DriverId, VehicleId, or BookingReference field — those are
/// never editable through this endpoint (see AdminBookingService).
/// </summary>
public class UpdateBookingRequest
{
    [Required]
    public Guid RouteId { get; set; }

    public DateOnly BookingDate { get; set; }
    public TimeOnly PickupTime { get; set; }

    [Required]
    public string PickupAddress { get; set; } = string.Empty;

    public int PassengerCount { get; set; }

    [Required]
    public string CustomerFirstName { get; set; } = string.Empty;

    [Required]
    public string CustomerLastName { get; set; } = string.Empty;

    [Required]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string CustomerPhone { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
