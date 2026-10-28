using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailMessageConfiguration : IEntityTypeConfiguration<CrmEmailMessage>
{
    public void Configure(EntityTypeBuilder<CrmEmailMessage> builder)
    {
        builder.ToTable("CrmEmailMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProviderMessageId).IsRequired().HasMaxLength(200);

        builder.Property(m => m.Direction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.FromAddress).IsRequired().HasMaxLength(320);
        builder.Property(m => m.FromName).HasMaxLength(200);

        // Small {{Address,Name}} JSON arrays — never a mailbox-wide blob (see EmailAddressValue).
        builder.Property(m => m.ToAddressesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.CcAddressesJson).HasColumnType("jsonb");
        builder.Property(m => m.BccAddressesJson).HasColumnType("jsonb");

        builder.Property(m => m.Subject).IsRequired().HasMaxLength(500);
        builder.Property(m => m.BodyHtml).HasColumnType("text");
        builder.Property(m => m.BodyPlainText).HasColumnType("text");
        builder.Property(m => m.OccurredAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(m => m.IsRead).IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(m => new { m.ConnectionId, m.ProviderMessageId }).IsUnique();
        builder.HasIndex(m => m.ThreadId);
        builder.HasIndex(m => m.OccurredAt);

        builder.HasOne<EmailProviderConnection>()
            .WithMany()
            .HasForeignKey(m => m.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EmailSendOperation>()
            .WithMany()
            .HasForeignKey(m => m.SendOperationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
