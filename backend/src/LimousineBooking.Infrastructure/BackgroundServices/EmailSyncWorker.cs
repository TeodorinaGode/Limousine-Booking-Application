using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.BackgroundServices;

/// <summary>
/// Polls the one CRM email connection (if any) and syncs it — identical shape
/// to NotificationOutboxWorker: a BackgroundService that opens its own DI
/// scope per poll cycle since the processor and its dependencies (DbContext,
/// etc.) are scoped, and a single bad poll never stops the worker permanently.
/// </summary>
public class EmailSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSyncSettings _settings;
    private readonly ILogger<EmailSyncWorker> _logger;

    public EmailSyncWorker(IServiceScopeFactory scopeFactory, IOptions<EmailSyncSettings> settings, ILogger<EmailSyncWorker> logger)
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
                await SyncOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Email sync poll failed; will retry on the next cycle.");
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

    private async Task SyncOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var connectionRepository = scope.ServiceProvider.GetRequiredService<IEmailProviderConnectionRepository>();

        var connection = await connectionRepository.GetAsync(stoppingToken);
        if (connection is null || connection.Status == EmailConnectionStatus.Disconnected)
            return;

        var processor = scope.ServiceProvider.GetRequiredService<IEmailSyncProcessor>();
        var syncedCount = await processor.SyncAsync(connection.Id, stoppingToken);

        if (syncedCount > 0)
            _logger.LogInformation("Email sync for connection {ConnectionId} synced {Count} new message(s).", connection.Id, syncedCount);
    }
}
