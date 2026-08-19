using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Drivers;

public class CreateDriverRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Required. Must be unique among Users (case-insensitive).</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Required. International numbers are accepted, not just Swiss ones.</summary>
    [Required]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Required. Hashed before storage — never returned, never logged.</summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional. Must reference an active vehicle with no other current driver.</summary>
    public Guid? VehicleId { get; set; }
}
