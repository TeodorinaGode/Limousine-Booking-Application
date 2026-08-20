namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Abstraction over the actual email transport. The application layer only ever
/// depends on this — never on SMTP/provider-specific types — so the provider
/// (SMTP today; SendGrid/Azure Communication Services later) can change without
/// touching any booking/notification business logic.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? plainTextBody, CancellationToken cancellationToken = default);
}
