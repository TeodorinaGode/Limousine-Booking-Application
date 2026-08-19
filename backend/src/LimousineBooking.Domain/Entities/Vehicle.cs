using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

public class Vehicle : AuditableEntity
{
    public string RegistrationNumber { get; private set; } = string.Empty;
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string VehicleType { get; private set; } = string.Empty;
    public int PassengerCapacity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

    private Vehicle()
    {
    }

    public Vehicle(string registrationNumber, string make, string model, string vehicleType, int passengerCapacity, string? notes = null)
    {
        Validate(registrationNumber, make, model, vehicleType, passengerCapacity);

        RegistrationNumber = registrationNumber;
        Make = make;
        Model = model;
        VehicleType = vehicleType;
        PassengerCapacity = passengerCapacity;
        Notes = notes;
    }

    /// <summary>
    /// Replaces every editable field except <see cref="IsActive"/> (used by
    /// administrator vehicle management) — use <see cref="Activate"/>/<see cref="Deactivate"/>.
    /// </summary>
    public void Update(string registrationNumber, string make, string model, string vehicleType, int passengerCapacity, string? notes)
    {
        Validate(registrationNumber, make, model, vehicleType, passengerCapacity);

        RegistrationNumber = registrationNumber;
        Make = make;
        Model = model;
        VehicleType = vehicleType;
        PassengerCapacity = passengerCapacity;
        Notes = notes;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void Validate(string registrationNumber, string make, string model, string vehicleType, int passengerCapacity)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("Make is required.", nameof(make));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(vehicleType))
            throw new ArgumentException("Vehicle type is required.", nameof(vehicleType));
        if (passengerCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(passengerCapacity), "Passenger capacity must be greater than zero.");
    }
}
