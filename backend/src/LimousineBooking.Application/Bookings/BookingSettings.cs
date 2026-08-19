namespace LimousineBooking.Application.Bookings;

/// <summary>Business rules for public booking creation, bound from the "BookingSettings" configuration section.</summary>
public class BookingSettings
{
    public const string SectionName = "BookingSettings";

    /// <summary>Minimum time between now and the requested pickup that a booking must leave.</summary>
    public int MinimumLeadTimeMinutes { get; set; } = 120;

    public int MaximumPassengers { get; set; } = 16;

    /// <summary>Minimum gap required between a driver's bookings before automatic assignment will use them back-to-back.</summary>
    public int DriverBufferMinutes { get; set; } = 15;
}
