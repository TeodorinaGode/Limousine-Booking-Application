namespace LimousineBooking.Application.Interfaces;

/// <summary>Processes one batch of due notifications. Extracted from the hosted background worker so it can be unit tested without a real host/timer.</summary>
public interface INotificationOutboxProcessor
{
    /// <summary>Returns the number of messages it attempted (sent or failed) — the caller loops while this is at least the batch size, to drain fully before waiting out the poll interval.</summary>
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default);
}
