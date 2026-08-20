namespace LimousineBooking.Application.Interfaces;

/// <summary>Processes one batch of pending contact-form submissions. Extracted from the hosted background worker so it can be unit tested without a real host/timer — mirrors INotificationOutboxProcessor's shape.</summary>
public interface IContactMessageOutboxProcessor
{
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default);
}
