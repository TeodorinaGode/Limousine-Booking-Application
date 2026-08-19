using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

public class Driver : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public Guid? CurrentVehicleId { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsActive { get; private set; } = true;

    public User? User { get; private set; }
    public Vehicle? CurrentVehicle { get; private set; }
    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public ICollection<DriverAvailability> Availabilities { get; private set; } = new List<DriverAvailability>();

    private Driver()
    {
    }

    public Driver(Guid userId, string phone)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ValidatePhone(phone);

        UserId = userId;
        Phone = phone;
    }

    public void UpdatePhone(string phone)
    {
        ValidatePhone(phone);
        Phone = phone;
    }

    public void AssignVehicle(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        CurrentVehicleId = vehicleId;
    }

    public void UnassignVehicle() => CurrentVehicleId = null;

    public void SetAvailable() => IsAvailable = true;

    public void SetUnavailable() => IsAvailable = false;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));
        if (!PhoneFormat.IsValid(phone))
            throw new ArgumentException("Phone number format is invalid.", nameof(phone));
    }
}
