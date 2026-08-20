using System.Net;
using System.Net.Mail;
using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Real email delivery via SMTP (.NET's built-in System.Net.Mail — no extra package
/// needed). Only ever constructed when EmailSettings:Enabled is true; see
/// DependencyInjection for the LoggingEmailService fallback used otherwise.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? plainTextBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
