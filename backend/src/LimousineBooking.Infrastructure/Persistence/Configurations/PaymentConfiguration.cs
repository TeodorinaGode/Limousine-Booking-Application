using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", t => t.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.ProviderPaymentId)
            .HasMaxLength(255);

        builder.Property(p => p.ProviderCheckoutSessionId)
            .HasMaxLength(255);

        builder.Property(p => p.CheckoutUrl)
            .HasMaxLength(2048);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.PaidAt)
            .HasColumnType("timestamptz");

        builder.Property(p => p.CheckoutExpiresAt)
            .HasColumnType("timestamptz");

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasIndex(p => p.BookingId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.PaidAt);

        // A given Stripe checkout session/payment intent must resolve to exactly
        // one Payment row — the webhook looks payments up by these (section 55/56).
        builder.HasIndex(p => p.ProviderCheckoutSessionId).IsUnique();
        builder.HasIndex(p => p.ProviderPaymentId).IsUnique();
    }
}
