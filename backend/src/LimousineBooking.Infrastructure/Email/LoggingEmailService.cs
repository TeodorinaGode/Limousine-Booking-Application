using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Development-mode "email provider" (EmailSettings:Enabled = false): no external
/// delivery is attempted. Logs just the recipient and subject — enough to verify
/// the notification flow end-to-end without a real SMTP account, and without
/// logging the full body (which could contain customer trip details). Always
/// "succeeds", so the outbox message is marked Sent exactly as it would be with
/// a real provider — the notification pipeline is fully exercisable in dev.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? plainTextBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] To: {To} | Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
