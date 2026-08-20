using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A public map pin for the service-area map (Prompt 19, section 16) —
/// marketing/reference geography (a city, airport, or nearby destination
/// ROI Limousinen serves), deliberately separate from <see cref="Route"/>.
/// A <see cref="Location"/> being on the map does not mean a bookable route
/// exists to/from it (section 25) — the frontend matches active routes to
/// locations by name to decide which pins get a "Book This Route" line, but
/// that matching never happens here in the domain model, keeping the two
/// concepts (service area vs. bookable route) structurally independent.
/// </summary>
public class Location : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public LocationType Type { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    private Location()
    {
    }

    public Location(string name, string countryCode, double latitude, double longitude, LocationType type, string? description, int sortOrder)
    {
        Validate(name, countryCode, latitude, longitude);

        Name = name;
        CountryCode = countryCode.ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        Type = type;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void Validate(string name, string countryCode, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new ArgumentException("Country code must be a 2-letter ISO code.", nameof(countryCode));
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
    }
}
