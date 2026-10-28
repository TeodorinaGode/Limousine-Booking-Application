namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Sanitizes untrusted HTML (synced email bodies) before it ever reaches a
/// response DTO. Split behind an interface so the Application layer never
/// takes a direct dependency on the concrete sanitizer library, same
/// convention as IPasswordService/IEmailTemplateRenderer.
/// </summary>
public interface IHtmlSanitizerService
{
    string Sanitize(string html);
}
