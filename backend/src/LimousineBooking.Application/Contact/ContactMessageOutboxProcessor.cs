using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Contact;

/// <summary>
/// Claims and sends one batch of due contact-form submissions to the company's
/// configured admin address (the same <c>NotificationSettings.AdminEmail</c>
/// operational-notifications recipient — no new configuration needed). Skips
/// gracefully (leaving the message Pending — never lost) if that address isn't
/// configured, mirroring <c>NotificationService.NotifyAdminManualAssignmentRequiredAsync</c>'s
/// precedent. This is the only place that ever calls <see cref="IEmailService"/>
/// for contact messages — nothing in the request path sends synchronously.
/// </summary>
public class ContactMessageOutboxProcessor : IContactMessageOutboxProcessor
{
    private const int BatchSize = 20;

    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationSettings _settings;
    private readonly ILogger<ContactMessageOutboxProcessor> _logger;

    public ContactMessageOutboxProcessor(
        IContactMessageRepository contactMessageRepository,
        IEmailService emailService,
        IEmailTemplateRenderer renderer,
        IDateTimeProvider dateTimeProvider,
        IOptions<NotificationSettings> settings,
        ILogger<ContactMessageOutboxProcessor> logger)
    {
        _contactMessageRepository = contactMessageRepository;
        _emailService = emailService;
        _renderer = renderer;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            _logger.LogWarning("Skipped processing pending contact messages — NotificationSettings:AdminEmail is not configured.");
            return 0;
        }

        var due = await _contactMessageRepository.GetPendingAsync(BatchSize, cancellationToken);

        foreach (var contactMessage in due)
        {
            try
            {
                var rendered = _renderer.Render("ContactMessageReceived", "en", new Dictionary<string, string>
                {
                    ["Name"] = contactMessage.Name,
                    ["Email"] = contactMessage.Email,
                    ["Phone"] = contactMessage.Phone ?? "(not provided)",
                    ["Subject"] = contactMessage.Subject,
                    ["Message"] = contactMessage.Message
                });

                await _emailService.SendAsync(_settings.AdminEmail, rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody, cancellationToken);

                contactMessage.MarkSent(_dateTimeProvider.UtcNow);
                _logger.LogInformation("Contact message {ContactMessageId} forwarded to the admin address.", contactMessage.Id);
            }
            catch (Exception ex)
            {
                contactMessage.MarkFailed(ex.Message);
                _logger.LogError(ex, "Failed to forward contact message {ContactMessageId}.", contactMessage.Id);
            }

            await _contactMessageRepository.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }
}
