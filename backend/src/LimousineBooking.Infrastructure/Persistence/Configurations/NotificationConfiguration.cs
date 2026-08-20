using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Recipient)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(n => n.NotificationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.SentAt)
            .HasColumnType("timestamptz");

        builder.Property(n => n.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(n => n.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(n => n.Payload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(n => n.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(n => n.NextAttemptAt)
            .HasColumnType("timestamptz");

        builder.Property(n => n.ProcessingStartedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(n => n.BookingId);

        // The background worker's main query: due Pending/stuck-Processing messages.
        builder.HasIndex(n => new { n.Status, n.NextAttemptAt });

        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => n.SentAt);
    }
}
