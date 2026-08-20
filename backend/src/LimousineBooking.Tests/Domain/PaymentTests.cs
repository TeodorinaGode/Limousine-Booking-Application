using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class PaymentTests
{
    private static Payment MakePayment(decimal amount = 150m) =>
        new(Guid.NewGuid(), PaymentProvider.Stripe, amount, "CHF");

    [Fact]
    public void Constructor_EmptyBookingId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Payment(Guid.Empty, PaymentProvider.Stripe, 100m, "CHF"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_AmountNotPositive_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Payment(Guid.NewGuid(), PaymentProvider.Stripe, amount, "CHF"));
    }

    [Fact]
    public void Constructor_MissingCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Payment(Guid.NewGuid(), PaymentProvider.Stripe, 100m, " "));
    }

    [Fact]
    public void NewPayment_StartsPending()
    {
        var payment = MakePayment();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void AttachCheckoutSession_SetsSessionFieldsWithoutChangingStatus()
    {
        var payment = MakePayment();
        var expires = DateTime.UtcNow.AddMinutes(15);

        payment.AttachCheckoutSession("cs_test_123", "https://checkout.example/cs_test_123", expires);

        Assert.Equal("cs_test_123", payment.ProviderCheckoutSessionId);
        Assert.Equal("https://checkout.example/cs_test_123", payment.CheckoutUrl);
        Assert.Equal(expires, payment.CheckoutExpiresAt);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void MarkPaid_SetsProviderPaymentIdStatusAndPaidAt()
    {
        var payment = MakePayment();
        var paidAt = DateTime.UtcNow;

        payment.MarkPaid("pi_test_123", paidAt);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("pi_test_123", payment.ProviderPaymentId);
        Assert.Equal(paidAt, payment.PaidAt);
        Assert.Null(payment.FailureReason);
    }

    [Fact]
    public void MarkPaid_MissingProviderPaymentId_Throws()
    {
        var payment = MakePayment();

        Assert.Throws<ArgumentException>(() => payment.MarkPaid(" ", DateTime.UtcNow));
    }

    [Fact]
    public void MarkPaid_AlreadyPaid_Throws()
    {
        var payment = MakePayment();
        payment.MarkPaid("pi_1", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => payment.MarkPaid("pi_2", DateTime.UtcNow));
    }

    [Fact]
    public void MarkFailed_SetsStatusAndReason()
    {
        var payment = MakePayment();

        payment.MarkFailed("card_declined");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("card_declined", payment.FailureReason);
    }

    [Fact]
    public void MarkFailed_OnPaidPayment_Throws()
    {
        var payment = MakePayment();
        payment.MarkPaid("pi_1", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => payment.MarkFailed("too_late"));
    }

    [Fact]
    public void MarkCancelled_SetsStatus()
    {
        var payment = MakePayment();

        payment.MarkCancelled();

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
    }

    [Fact]
    public void MarkCancelled_OnPaidPayment_Throws()
    {
        var payment = MakePayment();
        payment.MarkPaid("pi_1", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => payment.MarkCancelled());
    }

    [Fact]
    public void MarkRefunded_OnPaidPayment_SetsStatus()
    {
        var payment = MakePayment();
        payment.MarkPaid("pi_1", DateTime.UtcNow);

        payment.MarkRefunded();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Cancelled)]
    public void MarkRefunded_OnNonPaidPayment_Throws(PaymentStatus initialStatus)
    {
        var payment = MakePayment();
        switch (initialStatus)
        {
            case PaymentStatus.Failed:
                payment.MarkFailed("declined");
                break;
            case PaymentStatus.Cancelled:
                payment.MarkCancelled();
                break;
        }

        Assert.Throws<InvalidOperationException>(() => payment.MarkRefunded());
    }
}
