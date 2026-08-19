namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Minimal route projection for the public routes list — deliberately omits
/// audit fields (<c>CreatedAt</c>/<c>UpdatedAt</c>) and <c>IsActive</c>, since
/// only active routes are ever returned here.
/// </summary>
public class PublicRouteResponse
{
    public Guid Id { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}
