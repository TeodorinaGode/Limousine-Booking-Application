using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Routes;

public class CreateRouteRequest
{
    [Required]
    public string DepartureLocation { get; set; } = string.Empty;

    [Required]
    public string Destination { get; set; } = string.Empty;

    public int EstimatedDurationMinutes { get; set; }

    public decimal Price { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;
}
