namespace LimousineBooking.Application.Vehicles;

/// <summary>
/// Fleet-page-safe vehicle projection (Prompt 17, section 12) — deliberately
/// excludes everything an admin-facing <c>VehicleResponse</c> would include:
/// no <c>RegistrationNumber</c> (license plate), no <c>Notes</c> (internal),
/// no audit timestamps, no driver/booking associations. Only what a customer
/// deciding whether to book actually needs to see.
/// </summary>
public class PublicVehicleResponse
{
    public Guid Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public int PassengerCapacity { get; set; }
}
