namespace LimousineBooking.Application.Company;

/// <summary>
/// The company's public-facing identity/contact details, bound from the
/// "CompanySettings" configuration section (Prompt 17, section 16) — a single
/// source of truth instead of hard-coding phone/email/address across React
/// components. Every default below is a deliberately obvious placeholder (never
/// a plausible-looking fake business fact, per section 48) until the real
/// values are configured. Architected as plain configuration for now, matching
/// the spec's "first version may use application configuration" — the shape
/// here is exactly what a future Admin-editable "Company Information" screen
/// would read from and write to (section 49).
/// </summary>
public class CompanySettings
{
    public const string SectionName = "CompanySettings";

    public string CompanyName { get; set; } = "NOIR CHAUFFEUR";
    public string Tagline { get; set; } = "Private Chauffeur Service";
    public string Phone { get; set; } = "+41 00 000 00 00";
    public string Email { get; set; } = "info@example.com";
    public string Address { get; set; } = "[Company address not yet configured]";
    public string Website { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = "[Opening hours not yet configured]";
    public string EmergencyPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
