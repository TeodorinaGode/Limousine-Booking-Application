namespace LimousineBooking.Application.Map;

/// <summary>
/// Bundles map settings with the active pins in one response (Prompt 19,
/// section 15/16) — the map component always needs both to render, so this
/// avoids a second round trip. When <see cref="Enabled"/> is false, the
/// frontend must not render the map at all (and <see cref="Locations"/> may
/// still be populated — the flag is authoritative, not the array length).
/// </summary>
public class PublicLocationsResponse
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = string.Empty;
    public double DefaultLatitude { get; set; }
    public double DefaultLongitude { get; set; }
    public int DefaultZoom { get; set; }
    public IReadOnlyList<PublicLocationResponse> Locations { get; set; } = Array.Empty<PublicLocationResponse>();
}
