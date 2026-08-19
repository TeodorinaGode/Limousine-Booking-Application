using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Vehicles;

public class CreateVehicleRequest
{
    /// <summary>Required. Must be unique (normalized: trimmed, whitespace-collapsed, uppercased before comparison/storage).</summary>
    [Required]
    public string RegistrationNumber { get; set; } = string.Empty;

    /// <summary>Required, e.g. "Mercedes-Benz".</summary>
    [Required]
    public string Make { get; set; } = string.Empty;

    /// <summary>Required, e.g. "V-Class".</summary>
    [Required]
    public string Model { get; set; } = string.Empty;

    /// <summary>Required, e.g. "Sedan", "SUV", "Van", "Limousine", "Minivan".</summary>
    [Required]
    public string VehicleType { get; set; } = string.Empty;

    /// <summary>Must be greater than zero.</summary>
    public int PassengerCapacity { get; set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; set; }
}
