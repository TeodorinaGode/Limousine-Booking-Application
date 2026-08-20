using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("PaymentWebhookEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ProviderEventId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ReceivedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // The idempotency guarantee (section 23) — inserting a duplicate ProviderEventId
        // fails with a unique-constraint violation, which PaymentWebhookService treats
        // as "already processed".
        builder.HasIndex(e => e.ProviderEventId).IsUnique();
    }
}
