using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailTemplateConfiguration : IEntityTypeConfiguration<CrmEmailTemplate>
{
    public void Configure(EntityTypeBuilder<CrmEmailTemplate> builder)
    {
        builder.ToTable("CrmEmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
        builder.Property(t => t.BodyHtml).IsRequired().HasColumnType("text");
        builder.Property(t => t.Category).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Language).IsRequired().HasMaxLength(10);
        builder.Property(t => t.IsArchived).IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.IsArchived);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
