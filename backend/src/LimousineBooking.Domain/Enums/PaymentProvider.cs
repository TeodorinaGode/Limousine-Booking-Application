namespace LimousineBooking.Domain.Enums;

/// <summary>Only Stripe exists today; the enum (rather than a bare string) exists so a second provider can be added later without a data migration.</summary>
public enum PaymentProvider
{
    Stripe
}
