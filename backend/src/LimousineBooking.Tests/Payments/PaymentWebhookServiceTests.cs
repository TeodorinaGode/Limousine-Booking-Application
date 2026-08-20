using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainPayment = LimousineBooking.Domain.Entities.Payment;
using DomainRoute = LimousineBooking.Domain.Entities.Route;
using DomainWebhookEvent = LimousineBooking.Domain.Entities.PaymentWebhookEvent;

namespace LimousineBooking.Tests.Payments;

public class PaymentWebhookServiceTests
{
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookEventRepository = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ITransactionRunner> _transactionRunner = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public PaymentWebhookServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _webhookEventRepository.Setup(r => r.AddAsync(It.IsAny<DomainWebhookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactionRunner
            .Setup(t => t.RunSerializableAsync(It.IsAny<Func<CancellationToken, Task<PaymentWebhookOutcome>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<PaymentWebhookOutcome>> operation, CancellationToken ct) => operation(ct));
    }

    private PaymentWebhookService CreateService() => new(
        _paymentService.Object,
        _paymentRepository.Object,
        _webhookEventRepository.Object,
        _bookingRepository.Object,
        _notificationService.Object,
        _transactionRunner.Object,
        _dateTimeProvider.Object,
        Mock.Of<ILogger<PaymentWebhookService>>());

    private static (DomainBooking Booking, DomainRoute Route) MakeBooking(decimal price = 180.00m)
    {
        var route = new DomainRoute("Basel", "Zurich", 90, price, "CHF");
        var booking = new DomainBooking(
            $"LM-{Random.Shared.Next(100000, 999999)}",
            "Jane", "Doe", "jane.doe@example.com", "+41791234567",
            route.Id, new DateOnly(2026, 9, 10), new TimeOnly(14, 0),
            "Bahnhofplatz 1, Basel", 2, route.Price, route.Currency);

        SetProperty(booking, nameof(DomainBooking.Route), route);
        return (booking, route);
    }

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    [Fact]
    public async Task HandleWebhookAsync_InvalidSignature_ReturnsInvalidSignatureWithoutTouchingAnyRepository()
    {
        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidPaymentWebhookSignatureException("bad signature"));

        var outcome = await CreateService().HandleWebhookAsync("{}", "bad-sig");

        Assert.Equal(PaymentWebhookOutcome.InvalidSignature, outcome);
        _webhookEventRepository.Verify(r => r.AddAsync(It.IsAny<DomainWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhookAsync_CheckoutCompleted_MarksPaymentPaidAndNotifies()
    {
        var (booking, route) = MakeBooking();
        var payment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        payment.AttachCheckoutSession("cs_1", "https://checkout.example/cs_1", FixedUtcNow.AddMinutes(15));

        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_1",
                EventType = PaymentProviderEventType.CheckoutCompleted,
                CheckoutSessionId = "cs_1",
                ProviderPaymentId = "pi_1"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_1", It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.Processed, outcome);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("pi_1", payment.ProviderPaymentId);
        _notificationService.Verify(n => n.NotifyPaymentSucceededAsync(booking, route, payment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhookAsync_PaymentFailed_MarksPaymentFailedAndDoesNotNotify()
    {
        var payment = new DomainPayment(Guid.NewGuid(), PaymentProvider.Stripe, 180m, "CHF");
        payment.AttachCheckoutSession("cs_2", "https://checkout.example/cs_2", FixedUtcNow.AddMinutes(15));

        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_2",
                EventType = PaymentProviderEventType.PaymentFailed,
                CheckoutSessionId = "cs_2",
                FailureReason = "card_declined"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_2", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.Processed, outcome);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("card_declined", payment.FailureReason);
        _notificationService.Verify(n => n.NotifyPaymentSucceededAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhookAsync_CheckoutExpired_MarksPaymentCancelled()
    {
        var payment = new DomainPayment(Guid.NewGuid(), PaymentProvider.Stripe, 180m, "CHF");
        payment.AttachCheckoutSession("cs_3", "https://checkout.example/cs_3", FixedUtcNow.AddMinutes(15));

        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_3",
                EventType = PaymentProviderEventType.CheckoutExpired,
                CheckoutSessionId = "cs_3"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_3", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.Processed, outcome);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
    }

    [Fact]
    public async Task HandleWebhookAsync_UnknownCheckoutSession_AcknowledgesWithoutError()
    {
        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_4",
                EventType = PaymentProviderEventType.CheckoutCompleted,
                CheckoutSessionId = "cs_unknown"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_unknown", It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.Processed, outcome);
        _notificationService.Verify(n => n.NotifyPaymentSucceededAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhookAsync_AlreadyPaidPayment_EventReplayDoesNotRegressOrRenotify()
    {
        var (booking, route) = MakeBooking();
        var payment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        payment.AttachCheckoutSession("cs_5", "https://checkout.example/cs_5", FixedUtcNow.AddMinutes(15));
        payment.MarkPaid("pi_5", FixedUtcNow);

        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_5_replay",
                EventType = PaymentProviderEventType.CheckoutCompleted,
                CheckoutSessionId = "cs_5",
                ProviderPaymentId = "pi_5"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_5", It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _bookingRepository.Setup(r => r.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.Processed, outcome);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        _notificationService.Verify(n => n.NotifyPaymentSucceededAsync(It.IsAny<DomainBooking>(), It.IsAny<DomainRoute>(), It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhookAsync_DuplicateEventDelivery_ReturnsAlreadyProcessed()
    {
        var payment = new DomainPayment(Guid.NewGuid(), PaymentProvider.Stripe, 180m, "CHF");
        payment.AttachCheckoutSession("cs_6", "https://checkout.example/cs_6", FixedUtcNow.AddMinutes(15));

        _paymentService.Setup(s => s.ParseWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderWebhookEvent
            {
                ProviderEventId = "evt_6",
                EventType = PaymentProviderEventType.CheckoutCompleted,
                CheckoutSessionId = "cs_6",
                ProviderPaymentId = "pi_6"
            });
        _paymentRepository.Setup(r => r.GetByProviderCheckoutSessionIdAsync("cs_6", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var duplicateKeyException = new InvalidOperationException("unique constraint violation");
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(duplicateKeyException);
        _webhookEventRepository.Setup(r => r.IsDuplicateEventError(duplicateKeyException)).Returns(true);

        var outcome = await CreateService().HandleWebhookAsync("{}", "sig");

        Assert.Equal(PaymentWebhookOutcome.AlreadyProcessed, outcome);
    }
}
