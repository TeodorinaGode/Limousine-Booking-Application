namespace LimousineBooking.Application.Map;

/// <summary>Only customer-safe fields — never <c>SortOrder</c>/audit timestamps, matching <c>PublicVehicleResponse</c>'s precedent.</summary>
public class PublicLocationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}
