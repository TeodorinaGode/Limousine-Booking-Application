using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainPayment = LimousineBooking.Domain.Entities.Payment;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Tests.Payments;

public class PublicPaymentServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public PublicPaymentServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _paymentRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private PublicPaymentService CreateService(PaymentSettings? settings = null) => new(
        _bookingRepository.Object,
        _paymentRepository.Object,
        _paymentService.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new PaymentSettings
        {
            SuccessUrl = "https://app.example/payment/success",
            CancelUrl = "https://app.example/payment/cancelled",
            CheckoutExpirationMinutes = 15
        }),
        Mock.Of<ILogger<PublicPaymentService>>());

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

    private void SetupBooking(DomainBooking booking) =>
        _bookingRepository.Setup(r => r.GetByReferenceAsync(booking.BookingReference, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

    [Fact]
    public async Task CreatePaymentAsync_ValidBooking_CreatesPaymentUsingBookingPriceAndCurrency()
    {
        var (booking, _) = MakeBooking(price: 250.00m);
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        DomainPayment? capturedPayment = null;
        _paymentRepository.Setup(r => r.AddAsync(It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>()))
            .Callback<DomainPayment, CancellationToken>((p, _) => capturedPayment = p)
            .Returns(Task.CompletedTask);

        PaymentCheckoutRequest? capturedRequest = null;
        _paymentService.Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentCheckoutRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new PaymentCheckoutSession
            {
                ProviderCheckoutSessionId = "cs_test_1",
                CheckoutUrl = "https://checkout.example/cs_test_1",
                ExpiresAtUtc = FixedUtcNow.AddMinutes(15)
            });

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.True(result.Succeeded);
        Assert.Equal("https://checkout.example/cs_test_1", result.Checkout!.CheckoutUrl);
        Assert.Equal(250.00m, capturedPayment!.Amount);
        Assert.Equal("CHF", capturedPayment.Currency);
        Assert.Equal(250.00m, capturedRequest!.Amount);
        Assert.Equal("CHF", capturedRequest.Currency);
    }

    [Fact]
    public async Task CreatePaymentAsync_WrongAccessToken_ReturnsNotFound()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, "wrong-token");

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentError.NotFound, result.Error);
        Assert.Equal(PaymentErrorCodes.BookingNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_UnknownBookingReference_ReturnsNotFound()
    {
        _bookingRepository.Setup(r => r.GetByReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainBooking?)null);

        var result = await CreateService().CreatePaymentAsync("LM-999999", "any-token");

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentError.NotFound, result.Error);
    }

    [Fact]
    public async Task CreatePaymentAsync_CancelledBooking_ReturnsConflict()
    {
        var (booking, _) = MakeBooking();
        booking.Cancel("Customer request", Guid.NewGuid(), FixedUtcNow);
        SetupBooking(booking);

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentError.Conflict, result.Error);
        Assert.Equal(PaymentErrorCodes.BookingCancelled, result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_AlreadyPaidBooking_ReturnsConflict()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        var existingPaid = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        existingPaid.MarkPaid("pi_1", FixedUtcNow);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existingPaid);

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentError.Conflict, result.Error);
        Assert.Equal(PaymentErrorCodes.BookingAlreadyPaid, result.ErrorCode);
    }

    [Fact]
    public async Task CreatePaymentAsync_StillOpenSession_ReusesItInsteadOfCreatingANewOne()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var openPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        openPayment.AttachCheckoutSession("cs_open", "https://checkout.example/cs_open", FixedUtcNow.AddMinutes(10));
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(openPayment);

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.True(result.Succeeded);
        Assert.Equal("https://checkout.example/cs_open", result.Checkout!.CheckoutUrl);
        _paymentService.Verify(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<DomainPayment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePaymentAsync_ExpiredOpenSession_StartsANewAttemptInstead()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var expiredPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        expiredPayment.AttachCheckoutSession("cs_expired", "https://checkout.example/cs_expired", FixedUtcNow.AddMinutes(-1));
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expiredPayment);

        _paymentService.Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCheckoutSession { ProviderCheckoutSessionId = "cs_new", CheckoutUrl = "https://checkout.example/cs_new", ExpiresAtUtc = FixedUtcNow.AddMinutes(15) });

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.True(result.Succeeded);
        Assert.Equal("https://checkout.example/cs_new", result.Checkout!.CheckoutUrl);
    }

    [Fact]
    public async Task RetryPaymentAsync_NeverReusesAnOpenSession()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var openPayment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        openPayment.AttachCheckoutSession("cs_open", "https://checkout.example/cs_open", FixedUtcNow.AddMinutes(10));
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(openPayment);

        _paymentService.Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCheckoutSession { ProviderCheckoutSessionId = "cs_retry", CheckoutUrl = "https://checkout.example/cs_retry", ExpiresAtUtc = FixedUtcNow.AddMinutes(15) });

        var result = await CreateService().RetryPaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.True(result.Succeeded);
        Assert.Equal("https://checkout.example/cs_retry", result.Checkout!.CheckoutUrl);
        _paymentService.Verify(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentAsync_ProviderThrows_ReturnsProviderError()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetPaidByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        _paymentService.Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<PaymentCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider unreachable"));

        var result = await CreateService().CreatePaymentAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentError.ProviderError, result.Error);
        Assert.Equal(PaymentErrorCodes.PaymentProviderError, result.ErrorCode);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_ReturnsLatestPaymentStatus()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        var payment = new DomainPayment(booking.Id, PaymentProvider.Stripe, booking.Price, booking.Currency);
        payment.MarkPaid("pi_1", FixedUtcNow);
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var status = await CreateService().GetPaymentStatusAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.NotNull(status);
        Assert.Equal("Paid", status!.Status);
        Assert.Equal(booking.Price, status.Amount);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_WrongToken_ReturnsNull()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);

        var status = await CreateService().GetPaymentStatusAsync(booking.BookingReference, "not-the-token");

        Assert.Null(status);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_NoPaymentAttemptYet_ReturnsNull()
    {
        var (booking, _) = MakeBooking();
        SetupBooking(booking);
        _paymentRepository.Setup(r => r.GetLatestByBookingIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync((DomainPayment?)null);

        var status = await CreateService().GetPaymentStatusAsync(booking.BookingReference, booking.PublicAccessToken);

        Assert.Null(status);
    }
}
