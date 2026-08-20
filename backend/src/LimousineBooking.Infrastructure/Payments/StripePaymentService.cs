using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace LimousineBooking.Infrastructure.Payments;

/// <summary>
/// The only class in this application that references the Stripe SDK directly
/// (section 4/73) — everything above IPaymentService speaks the provider-agnostic
/// contracts in LimousineBooking.Application.Payments.PaymentProviderContracts.
/// Uses Stripe-hosted Checkout Sessions (never collects card data itself).
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly PaymentSettings _settings;
    private readonly StripeClient _client;

    public StripePaymentService(IOptions<PaymentSettings> settings)
    {
        _settings = settings.Value;
        _client = new StripeClient(_settings.SecretKey);
    }

    public async Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            CustomerEmail = request.CustomerEmail,
            ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        // Stripe currency codes are lowercase ISO 4217 (e.g. "chf") —
                        // never rely on Stripe's own default currency (section 6).
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = ToSmallestCurrencyUnit(request.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Description
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["bookingId"] = request.BookingId.ToString(),
                ["bookingReference"] = request.BookingReference,
                ["paymentId"] = request.PaymentId.ToString()
            }
        };

        var service = new SessionService(_client);
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new PaymentCheckoutSession
        {
            ProviderCheckoutSessionId = session.Id,
            CheckoutUrl = session.Url,
            ExpiresAtUtc = session.ExpiresAt
        };
    }

    public Task<PaymentProviderWebhookEvent> ParseWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _settings.WebhookSecret);
        }
        catch (StripeException ex)
        {
            throw new InvalidPaymentWebhookSignatureException("Stripe webhook signature verification failed.", ex);
        }

        var eventType = stripeEvent.Type switch
        {
            "checkout.session.completed" => PaymentProviderEventType.CheckoutCompleted,
            "checkout.session.expired" => PaymentProviderEventType.CheckoutExpired,
            "checkout.session.async_payment_failed" => PaymentProviderEventType.PaymentFailed,
            _ => PaymentProviderEventType.Unknown
        };

        if (stripeEvent.Data.Object is not Session session)
        {
            return Task.FromResult(new PaymentProviderWebhookEvent
            {
                ProviderEventId = stripeEvent.Id,
                EventType = PaymentProviderEventType.Unknown
            });
        }

        return Task.FromResult(new PaymentProviderWebhookEvent
        {
            ProviderEventId = stripeEvent.Id,
            EventType = eventType,
            CheckoutSessionId = session.Id,
            ProviderPaymentId = session.PaymentIntentId,
            FailureReason = eventType == PaymentProviderEventType.PaymentFailed ? "Asynchronous payment method failed." : null
        });
    }

    public async Task<PaymentProviderRefund> RefundAsync(string providerPaymentId, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = providerPaymentId,
            Amount = ToSmallestCurrencyUnit(amount)
        };

        var service = new RefundService(_client);
        var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new PaymentProviderRefund
        {
            ProviderRefundId = refund.Id,
            Succeeded = refund.Status is "succeeded" or "pending"
        };
    }

    /// <summary>
    /// Converts a decimal CHF amount to Stripe's smallest currency unit (Rappen/cents).
    /// Deterministic decimal arithmetic only — never float/double (section 7).
    /// </summary>
    private static long ToSmallestCurrencyUnit(decimal amount) =>
        (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
}
