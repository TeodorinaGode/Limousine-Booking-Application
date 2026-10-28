using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class EmailSendOperationConfiguration : IEntityTypeConfiguration<EmailSendOperation>
{
    public void Configure(EntityTypeBuilder<EmailSendOperation> builder)
    {
        builder.ToTable("EmailSendOperations");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.ClientOperationId).IsUnique();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.GeneratedMessageIdHeader).IsRequired().HasMaxLength(300);
        builder.Property(s => s.ProviderMessageId).HasMaxLength(200);
        builder.Property(s => s.ErrorMessage).HasMaxLength(2000);

        builder.Property(s => s.ToAddressesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(s => s.CcAddressesJson).HasColumnType("jsonb");
        builder.Property(s => s.BccAddressesJson).HasColumnType("jsonb");

        builder.Property(s => s.Subject).HasMaxLength(500);
        builder.Property(s => s.BodyHtml).IsRequired().HasColumnType("text");

        builder.Property(s => s.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(s => s.CompanyId);
        builder.HasIndex(s => s.ContactId);
        builder.HasIndex(s => s.DealId);

        builder.HasOne<EmailProviderConnection>()
            .WithMany()
            .HasForeignKey(s => s.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CrmEmailTemplate>()
            .WithMany()
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CrmCompany>()
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CrmContact>()
            .WithMany()
            .HasForeignKey(s => s.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CrmDeal>()
            .WithMany()
            .HasForeignKey(s => s.DealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
