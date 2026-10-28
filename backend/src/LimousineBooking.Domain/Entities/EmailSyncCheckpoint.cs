using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// One sync watermark + run-lock per connection. IsRunning/RunStartedAt is the
/// same "stale processing is reclaimed after N minutes" lock already proven by
/// NotificationOutboxProcessor — it prevents two overlapping EmailSyncWorker
/// passes from corrupting the incremental-sync cursor.
/// </summary>
public class EmailSyncCheckpoint : AuditableEntity
{
    public Guid ConnectionId { get; private set; }
    public string? ProviderCursor { get; private set; }
    public bool IsRunning { get; private set; }
    public DateTime? RunStartedAt { get; private set; }
    public EmailSyncRunStatus LastRunStatus { get; private set; } = EmailSyncRunStatus.Success;
    public DateTime? LastRunAt { get; private set; }
    public string? LastError { get; private set; }
    public int MessagesSyncedLastRun { get; private set; }

    public EmailProviderConnection? Connection { get; private set; }

    private EmailSyncCheckpoint()
    {
    }

    public EmailSyncCheckpoint(Guid connectionId)
    {
        if (connectionId == Guid.Empty)
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        ConnectionId = connectionId;
    }

    /// <summary>True if no run is currently claimed, or the claimed run is older than staleRunningMinutes (assumed crashed).</summary>
    public bool IsClaimable(DateTime nowUtc, int staleRunningMinutes) =>
        !IsRunning || (RunStartedAt.HasValue && (nowUtc - RunStartedAt.Value).TotalMinutes > staleRunningMinutes);

    public void BeginRun(DateTime nowUtc)
    {
        IsRunning = true;
        RunStartedAt = nowUtc;
    }

    /// <summary>newCursor is null when the run found nothing new — the existing cursor is still valid and is left untouched.</summary>
    public void CompleteRun(DateTime nowUtc, string? newCursor, int messagesSynced)
    {
        IsRunning = false;
        RunStartedAt = null;
        LastRunStatus = EmailSyncRunStatus.Success;
        LastRunAt = nowUtc;
        LastError = null;
        MessagesSyncedLastRun = messagesSynced;
        if (newCursor is not null)
            ProviderCursor = newCursor;
    }

    public void FailRun(DateTime nowUtc, string errorMessage)
    {
        IsRunning = false;
        RunStartedAt = null;
        LastRunStatus = EmailSyncRunStatus.Failed;
        LastRunAt = nowUtc;
        LastError = errorMessage;
    }

    /// <summary>Gmail expired the stored historyId (410 Gone) — the next run must do one full resync and start a fresh cursor.</summary>
    public void InvalidateCursor() => ProviderCursor = null;
}
