namespace LimousineBooking.Application.Drivers;

/// <summary>
/// Used for both the driver list and driver-details endpoints — the fields
/// required by each (per the spec) are identical, so one DTO covers both
/// rather than duplicating an otherwise-identical type.
/// </summary>
public class DriverResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public DriverVehicleSummary? Vehicle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
