namespace LimousineBooking.Application.Company;

/// <summary>
/// The company's public-facing identity/contact details, bound from the
/// "CompanySettings" configuration section (Prompt 17, section 16) — a single
/// source of truth instead of hard-coding phone/email/address across React
/// components. Architected as plain configuration for now, matching the
/// spec's "first version may use application configuration" — the shape here
/// is exactly what a future Admin-editable "Company Information" screen would
/// read from and write to (section 49).
///
/// Defaults below are the real ROI Limousinen business identity, migrated
/// from the company's existing website (roi-limousinen.ch) per Prompt 18 —
/// verified live against that site rather than invented. <see cref="Address"/>
/// and <see cref="OpeningHours"/> remain honest placeholders because the old
/// site never published a street address or opening hours (only "based in
/// Switzerland" and a phone/email) — see the README's Prompt 18 "requires
/// business confirmation" list.
/// </summary>
public class CompanySettings
{
    public const string SectionName = "CompanySettings";

    public string CompanyName { get; set; } = "ROI Limousinen";
    public string Tagline { get; set; } = "Premium / Luxury Chauffeur Service";
    public string Phone { get; set; } = "+41 78 264 85 85";
    public string Email { get; set; } = "contact@roi-limousinen.ch";
    public string Address { get; set; } = "Switzerland";
    public string Website { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = "[Opening hours not yet configured]";
    public string EmergencyPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 codes, in the order the old site listed them —
    /// deliberately codes, not English display names, so the frontend can
    /// render a properly translated country name per language (section
    /// 21/22) instead of showing "Germany" while the rest of the page is in
    /// German. Defaults to an empty list, not the real Switzerland/Austria/…
    /// values, on purpose: ASP.NET Core's <c>ConfigurationBinder</c> appends
    /// config array entries onto an already-populated default <c>List&lt;T&gt;</c>
    /// rather than replacing it, which would silently double every entry
    /// (caught live — <c>GET /api/public/company</c> returned 14 codes
    /// instead of 7). appsettings.json is the actual source of the real
    /// values.
    /// </summary>
    public List<string> OperatingCountryCodes { get; set; } = new();

    /// <summary>Existing official social links only (section 17) — never invented.</summary>
    public string FacebookUrl { get; set; } = "https://www.facebook.com/roi.limousinen";
    public string InstagramUrl { get; set; } = "https://www.instagram.com/roi.limousinen/";
    public string WhatsAppUrl { get; set; } = "https://wa.me/+410782648585";
}
