using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Drivers;

public class UpdateDriverRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    /// <summary>Null unassigns the current vehicle. Must reference an active vehicle with no other current driver.</summary>
    public Guid? VehicleId { get; set; }
}
