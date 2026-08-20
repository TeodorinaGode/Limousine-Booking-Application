using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IPaymentWebhookEventRepository
{
    /// <summary>Tracks the new event in this unit of work — the actual dedup guarantee comes from the unique index on ProviderEventId plus the caller's SaveChangesAsync, not from a pre-check here (see PaymentWebhookEvent's summary).</summary>
    Task AddAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if <paramref name="exception"/> (thrown from a repository's SaveChangesAsync
    /// in the same unit of work as <see cref="AddAsync"/>) was caused by the unique
    /// index on ProviderEventId — i.e. this exact provider event was already recorded
    /// by an earlier or concurrent delivery. Keeps the Npgsql-specific detection out of
    /// the Application layer, which only ever sees this yes/no answer.
    /// </summary>
    bool IsDuplicateEventError(Exception exception);
}
