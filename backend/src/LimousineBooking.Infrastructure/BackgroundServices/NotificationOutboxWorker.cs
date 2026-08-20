using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.BackgroundServices;

/// <summary>
/// Polls for due notifications and sends them. This is the ONLY thing in the
/// application that ever calls IEmailService — nothing in an HTTP request path
/// sends synchronously, so an email provider outage can never fail a booking
/// request (see Notification's summary for the full transactional-outbox story).
/// Registered as a singleton hosted service; creates a new DI scope per poll
/// cycle since the processor and its dependencies (DbContext, etc.) are scoped.
/// </summary>
public class NotificationOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationOutboxWorker> _logger;

    public NotificationOutboxWorker(IServiceScopeFactory scopeFactory, IOptions<NotificationSettings> settings, ILogger<NotificationOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single bad poll (e.g. the database briefly unreachable) must
                // never stop the worker permanently — log and try again next cycle.
                _logger.LogError(ex, "Notification outbox poll failed; will retry on the next cycle.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Keeps processing batches until a batch comes back empty, so a burst of enqueued notifications doesn't have to wait multiple poll intervals to drain.</summary>
    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<INotificationOutboxProcessor>();

            var processedCount = await processor.ProcessBatchAsync(stoppingToken);
            if (processedCount == 0)
                break;
        }
    }
}
