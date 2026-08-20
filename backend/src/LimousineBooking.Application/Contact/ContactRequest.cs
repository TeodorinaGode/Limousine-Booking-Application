namespace LimousineBooking.Application.Contact;

/// <summary>Public contact-form submission. The customer needs no account (Prompt 17, section 18).</summary>
public class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
