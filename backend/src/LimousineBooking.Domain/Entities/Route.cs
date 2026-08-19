using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A predefined limousine trip (e.g. "Basel to Zurich"). Holds no specific
/// date/time — that belongs to <see cref="Booking"/>.
/// </summary>
public class Route : AuditableEntity
{
    public string DepartureLocation { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public int EstimatedDurationMinutes { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

    private Route()
    {
    }

    public Route(string departureLocation, string destination, int estimatedDurationMinutes, decimal price, string currency)
    {
        if (string.IsNullOrWhiteSpace(departureLocation))
            throw new ArgumentException("Departure location is required.", nameof(departureLocation));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination is required.", nameof(destination));
        if (estimatedDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedDurationMinutes), "Estimated duration must be greater than zero.");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must not be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        DepartureLocation = departureLocation;
        Destination = destination;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Price = price;
        Currency = currency;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Price must not be negative.");

        Price = newPrice;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
