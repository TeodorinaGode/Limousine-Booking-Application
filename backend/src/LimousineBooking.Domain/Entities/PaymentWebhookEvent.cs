using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// Insert-only idempotency record of every processed provider webhook event.
/// ProviderEventId has a unique database index (see PaymentWebhookEventConfiguration) —
/// attempting to insert one already seen fails the SaveChangesAsync call with a
/// unique-constraint violation, which the caller (PaymentWebhookService) treats as
/// "already processed, do nothing further" and acknowledges to the provider without
/// re-applying any business effect (no duplicate payments, notifications, or booking
/// updates), even under concurrent delivery of the same event.
/// </summary>
public class PaymentWebhookEvent : Entity
{
    public string Provider { get; private set; } = string.Empty;
    public string ProviderEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTime ReceivedAt { get; private set; }

    private PaymentWebhookEvent()
    {
    }

    public PaymentWebhookEvent(string provider, string providerEventId, string eventType, DateTime receivedAt)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(providerEventId))
            throw new ArgumentException("ProviderEventId is required.", nameof(providerEventId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required.", nameof(eventType));

        Provider = provider;
        ProviderEventId = providerEventId;
        EventType = eventType;
        ReceivedAt = receivedAt;
    }
}
