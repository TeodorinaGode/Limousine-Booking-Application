using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.BackgroundServices;

/// <summary>
/// Polls for pending contact-form submissions and forwards them to the admin
/// address. Mirrors <see cref="NotificationOutboxWorker"/>'s shape exactly
/// (same poll interval setting, same singleton-hosted-service-creates-a-scope-
/// per-cycle pattern) — kept as its own small worker rather than folded into
/// the booking-notification worker, since <see cref="IContactMessageOutboxProcessor"/>
/// has nothing to do with bookings.
/// </summary>
public class ContactMessageOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationSettings _settings;
    private readonly ILogger<ContactMessageOutboxWorker> _logger;

    public ContactMessageOutboxWorker(IServiceScopeFactory scopeFactory, IOptions<NotificationSettings> settings, ILogger<ContactMessageOutboxWorker> logger)
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
                _logger.LogError(ex, "Contact message outbox poll failed; will retry on the next cycle.");
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

    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IContactMessageOutboxProcessor>();

            var processedCount = await processor.ProcessBatchAsync(stoppingToken);
            if (processedCount == 0)
                break;
        }
    }
}
