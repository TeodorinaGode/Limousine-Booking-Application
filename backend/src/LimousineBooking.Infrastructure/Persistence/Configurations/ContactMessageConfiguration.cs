using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.PreferredContactMethod)
            .HasMaxLength(10);

        builder.Property(c => c.PreferredDate)
            .HasColumnType("date");

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(c => c.SentAt)
            .HasColumnType("timestamptz");

        // The outbox worker's due-message query (Status = Pending, oldest first).
        builder.HasIndex(c => new { c.Status, c.CreatedAt });
    }
}
