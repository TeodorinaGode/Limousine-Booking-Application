using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Drivers;

public class ResetDriverPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
