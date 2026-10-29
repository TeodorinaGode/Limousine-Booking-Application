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

        // Gmail's attachmentId is an opaque, undocumented-length string — in
        // practice it regularly exceeds 300 characters, which is what broke
        // a real sync. Unbounded, same as BodyHtml/BodyPlainText, rather than
        // guessing another fixed cap that could just as easily be too small.
        builder.Property(a => a.ProviderAttachmentId).IsRequired().HasColumnType("text");

        builder.Property(a => a.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(a => a.MessageId);
    }
}
