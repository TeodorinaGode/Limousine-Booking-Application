namespace LimousineBooking.Application.Map;

/// <summary>
/// Map rendering defaults, bound from the "MapSettings" configuration
/// section (Prompt 19, section 15) — mirrors <c>CompanySettings</c>'s
/// pattern of keeping deployment-tunable, non-secret values in config
/// rather than hard-coded in React. <see cref="Provider"/> documents which
/// tile provider the frontend's <c>ServiceAreaMap</c> is currently wired to;
/// changing it here does not itself switch providers (that requires a
/// frontend code change, per the abstraction in section 13) — it exists so
/// the active provider is visible/auditable from configuration.
/// <see cref="Enabled"/> is a kill switch: the frontend never renders the
/// map (and never loads the map library) when this is false.
/// </summary>
public class MapSettings
{
    public const string SectionName = "MapSettings";

    public string Provider { get; set; } = "leaflet";
    public double DefaultLatitude { get; set; } = 47.0;
    public double DefaultLongitude { get; set; } = 8.5;
    public int DefaultZoom { get; set; } = 6;
    public bool Enabled { get; set; } = true;
}
