using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Public booking submission. Contains only what an anonymous customer supplies —
/// price, currency, status, driver/vehicle, and the booking reference are all
/// determined server-side and are never accepted from the client.
/// </summary>
public class CreateBookingRequest
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

    /// <summary>
    /// The language the customer was using in the booking form (en/de/fr) — captured
    /// so their confirmation email uses the same language later, regardless of what their
    /// browser is set to by then. Optional; an unsupported/missing value silently falls
    /// back to English (see Booking's constructor / SupportedLanguages.Normalize) rather
    /// than failing the booking.
    /// </summary>
    public string? LanguageCode { get; set; }
}
