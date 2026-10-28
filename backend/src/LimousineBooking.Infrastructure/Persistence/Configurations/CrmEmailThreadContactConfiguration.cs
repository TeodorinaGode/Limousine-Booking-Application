using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailThreadContactConfiguration : IEntityTypeConfiguration<CrmEmailThreadContact>
{
    public void Configure(EntityTypeBuilder<CrmEmailThreadContact> builder)
    {
        builder.ToTable("CrmEmailThreadContacts");

        builder.HasKey(tc => tc.Id);

        builder.HasIndex(tc => new { tc.ThreadId, tc.ContactId }).IsUnique();

        builder.Property(tc => tc.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(tc => tc.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(tc => tc.Contact)
            .WithMany()
            .HasForeignKey(tc => tc.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
