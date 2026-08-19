using System.ComponentModel.DataAnnotations;

namespace LimousineBooking.Application.Bookings;

public class AssignDriverRequest
{
    [Required]
    public Guid DriverId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }
}
