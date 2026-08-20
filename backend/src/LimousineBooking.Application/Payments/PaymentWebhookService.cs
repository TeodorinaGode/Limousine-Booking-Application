using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using DomainWebhookEvent = LimousineBooking.Domain.Entities.PaymentWebhookEvent;

namespace LimousineBooking.Application.Payments;

/// <inheritdoc cref="IPaymentWebhookService" />
public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentWebhookEventRepository _webhookEventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly INotificationService _notificationService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IPaymentWebhookEventRepository webhookEventRepository,
        IBookingRepository bookingRepository,
        INotificationService notificationService,
        ITransactionRunner transactionRunner,
        IDateTimeProvider dateTimeProvider,
        ILogger<PaymentWebhookService> logger)
    {
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _webhookEventRepository = webhookEventRepository;
        _bookingRepository = bookingRepository;
        _notificationService = notificationService;
        _transactionRunner = transactionRunner;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<PaymentWebhookOutcome> HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        PaymentProviderWebhookEvent providerEvent;
        try
        {
            providerEvent = await _paymentService.ParseWebhookEventAsync(payload, signatureHeader, cancellationToken);
        }
        catch (InvalidPaymentWebhookSignatureException ex)
        {
            _logger.LogWarning(ex, "Rejected a payment webhook — signature verification failed.");
            return PaymentWebhookOutcome.InvalidSignature;
        }

        return await _transactionRunner.RunSerializableAsync(ct => ApplyEventAsync(providerEvent, ct), cancellationToken);
    }

    private async Task<PaymentWebhookOutcome> ApplyEventAsync(PaymentProviderWebhookEvent providerEvent, CancellationToken cancellationToken)
    {
        await _webhookEventRepository.AddAsync(
            new DomainWebhookEvent("Stripe", providerEvent.ProviderEventId, providerEvent.EventType.ToString(), _dateTimeProvider.UtcNow),
            cancellationToken);

        var payment = providerEvent.CheckoutSessionId is null
            ? null
            : await _paymentRepository.GetByProviderCheckoutSessionIdAsync(providerEvent.CheckoutSessionId, cancellationToken);

        if (payment is null)
        {
            _logger.LogInformation(
                "Payment webhook event {EventId} ({EventType}) referred to an unknown or foreign checkout session; acknowledging with no action.",
                providerEvent.ProviderEventId, providerEvent.EventType);
        }
        else
        {
            await ApplyToPaymentAsync(payment, providerEvent, cancellationToken);
        }

        try
        {
            await _paymentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_webhookEventRepository.IsDuplicateEventError(ex))
        {
            _logger.LogInformation("Payment webhook event {EventId} was already processed; ignoring the duplicate delivery.", providerEvent.ProviderEventId);
            return PaymentWebhookOutcome.AlreadyProcessed;
        }

        return PaymentWebhookOutcome.Processed;
    }

    private async Task ApplyToPaymentAsync(Domain.Entities.Payment payment, PaymentProviderWebhookEvent providerEvent, CancellationToken cancellationToken)
    {
        // Defense-in-depth beyond the event-id dedup above: a payment already in a
        // terminal state never regresses, however many times an event replays.
        var isOpen = payment.Status is PaymentStatus.Pending or PaymentStatus.Processing;

        switch (providerEvent.EventType)
        {
            case PaymentProviderEventType.CheckoutCompleted when isOpen:
                payment.MarkPaid(providerEvent.ProviderPaymentId ?? providerEvent.CheckoutSessionId!, _dateTimeProvider.UtcNow);

                var booking = await _bookingRepository.GetByIdWithDetailsAsync(payment.BookingId, cancellationToken);
                if (booking?.Route is not null)
                    await _notificationService.NotifyPaymentSucceededAsync(booking, booking.Route, payment, cancellationToken);
                break;

            case PaymentProviderEventType.PaymentFailed when isOpen:
                payment.MarkFailed(providerEvent.FailureReason ?? "The payment could not be completed.");
                break;

            case PaymentProviderEventType.CheckoutExpired when isOpen:
                payment.MarkCancelled();
                break;
        }
    }
}
