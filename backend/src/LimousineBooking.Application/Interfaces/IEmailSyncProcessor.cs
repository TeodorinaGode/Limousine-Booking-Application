namespace LimousineBooking.Application.Interfaces;

/// <summary>One incremental sync pass for one connection. Extracted from the hosted background worker so it can be unit tested without a real host/timer — same split as INotificationOutboxProcessor.</summary>
public interface IEmailSyncProcessor
{
    /// <summary>Returns the number of new messages synced. Returns 0 (without error) if the connection is disconnected or another run already owns the checkpoint.</summary>
    Task<int> SyncAsync(Guid connectionId, CancellationToken cancellationToken = default);
}
