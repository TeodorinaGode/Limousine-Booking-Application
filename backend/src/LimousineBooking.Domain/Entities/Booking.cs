using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

public class Booking : AuditableEntity
{
    public string BookingReference { get; private set; } = string.Empty;

    public string CustomerFirstName { get; private set; } = string.Empty;
    public string CustomerLastName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;

    public Guid RouteId { get; private set; }
    public DateOnly TravelDate { get; private set; }
    public TimeOnly PickupTime { get; private set; }
    public string PickupAddress { get; private set; } = string.Empty;
    public int PassengerCount { get; private set; }
    public string? Notes { get; private set; }

    public Guid? DriverId { get; private set; }
    public Guid? VehicleId { get; private set; }

    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    public Route? Route { get; private set; }
    public Driver? Driver { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public ICollection<BookingStatusHistory> StatusHistory { get; private set; } = new List<BookingStatusHistory>();
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    private Booking()
    {
    }

    public Booking(
        string bookingReference,
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string customerPhone,
        Guid routeId,
        DateOnly travelDate,
        TimeOnly pickupTime,
        string pickupAddress,
        int passengerCount,
        decimal price,
        string currency,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(bookingReference))
            throw new ArgumentException("Booking reference is required.", nameof(bookingReference));
        if (string.IsNullOrWhiteSpace(customerFirstName))
            throw new ArgumentException("Customer first name is required.", nameof(customerFirstName));
        if (string.IsNullOrWhiteSpace(customerLastName))
            throw new ArgumentException("Customer last name is required.", nameof(customerLastName));
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ArgumentException("Customer phone is required.", nameof(customerPhone));
        if (routeId == Guid.Empty)
            throw new ArgumentException("RouteId is required.", nameof(routeId));
        if (string.IsNullOrWhiteSpace(pickupAddress))
            throw new ArgumentException("Pickup address is required.", nameof(pickupAddress));
        if (passengerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(passengerCount), "Passenger count must be greater than zero.");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must not be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        BookingReference = bookingReference;
        CustomerFirstName = customerFirstName;
        CustomerLastName = customerLastName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        RouteId = routeId;
        TravelDate = travelDate;
        PickupTime = pickupTime;
        PickupAddress = pickupAddress;
        PassengerCount = passengerCount;
        Price = price;
        Currency = currency;
        Notes = notes;
        Status = BookingStatus.Pending;
    }

    public void AssignDriver(Guid driverId)
    {
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        DriverId = driverId;
    }

    public void AssignVehicle(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        VehicleId = vehicleId;
    }

    public void ChangeStatus(BookingStatus newStatus) => Status = newStatus;
}
