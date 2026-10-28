using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailAttachmentConfiguration : IEntityTypeConfiguration<CrmEmailAttachment>
{
    public void Configure(EntityTypeBuilder<CrmEmailAttachment> builder)
    {
        builder.ToTable("CrmEmailAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(300);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(a => a.SizeBytes).IsRequired();
        builder.Property(a => a.ProviderAttachmentId).IsRequired().HasMaxLength(300);

        builder.Property(a => a.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(a => a.MessageId);
    }
}
