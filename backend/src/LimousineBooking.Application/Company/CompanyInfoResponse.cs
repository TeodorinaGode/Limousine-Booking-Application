namespace LimousineBooking.Application.Company;

public class CompanyInfoResponse
{
    public string CompanyName { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string? EmergencyPhone { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<string> OperatingCountryCodes { get; set; } = Array.Empty<string>();
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WhatsAppUrl { get; set; }
}
