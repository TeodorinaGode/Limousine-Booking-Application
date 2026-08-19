namespace LimousineBooking.Application.Routes;

public class RouteResponse
{
    public Guid Id { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
