using System.Text.Json;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Notifications;

/// <summary>
/// Claims and sends one batch of due notifications. This is the only place that
/// ever calls IEmailService — the request path (booking/assignment/cancel
/// operations) only ever enqueues via INotificationService, never sends. Batch
/// size and retry backoff come from NotificationSettings, never hard-coded.
/// </summary>
public class NotificationOutboxProcessor : INotificationOutboxProcessor
{
    private const int BatchSize = 20;

    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationOutboxProcessor> _logger;

    public NotificationOutboxProcessor(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationOutboxProcessor> logger)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var staleProcessingBefore = now.AddMinutes(-_settings.StaleProcessingMinutes);

        var due = await _notificationRepository.GetDueForProcessingAsync(now, staleProcessingBefore, BatchSize, cancellationToken);

        var backoff = (_settings.RetryBackoffMinutes.Length > 0 ? _settings.RetryBackoffMinutes : new[] { 1, 5, 15, 30, 60 })
            .Select(m => TimeSpan.FromMinutes(m))
            .ToList();

        foreach (var notification in due)
        {
            // Claim it before sending — even if the process crashes mid-send,
            // GetDueForProcessingAsync will re-claim it once it's stale rather
            // than leaving it silently stuck forever.
            notification.MarkProcessing(_dateTimeProvider.UtcNow);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processing notification {NotificationId} ({NotificationType}) for booking {BookingId}.",
                notification.Id, notification.NotificationType, notification.BookingId);

            try
            {
                var payload = JsonSerializer.Deserialize<NotificationPayload>(notification.Payload)
                    ?? throw new InvalidOperationException("Notification payload could not be deserialized.");

                await _emailService.SendAsync(payload.RecipientEmail, payload.Subject, payload.HtmlBody, payload.PlainTextBody, cancellationToken);

                notification.MarkSent(_dateTimeProvider.UtcNow);
                _logger.LogInformation("Notification {NotificationId} sent to {Recipient}.", notification.Id, MaskEmail(payload.RecipientEmail));
            }
            catch (Exception ex)
            {
                notification.MarkFailed(ex.Message, _dateTimeProvider.UtcNow, _settings.MaxRetries, backoff);

                if (notification.Status == NotificationStatus.Failed)
                    _logger.LogError(ex, "Notification {NotificationId} permanently failed after {RetryCount} attempt(s).", notification.Id, notification.RetryCount);
                else
                    _logger.LogWarning(ex, "Notification {NotificationId} failed, retry {RetryCount} scheduled for {NextAttemptAt}.", notification.Id, notification.RetryCount, notification.NextAttemptAt);
            }

            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }

    /// <summary>Never log a full email address at Information level — just enough to spot-check in dev without treating logs as a customer PII store.</summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex <= 1 ? "***" : $"{email[0]}***{email[atIndex..]}";
    }
}
