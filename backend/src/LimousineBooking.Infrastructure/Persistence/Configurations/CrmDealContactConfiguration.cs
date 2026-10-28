using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmDealContactConfiguration : IEntityTypeConfiguration<CrmDealContact>
{
    public void Configure(EntityTypeBuilder<CrmDealContact> builder)
    {
        builder.ToTable("CrmDealContacts");

        builder.HasKey(dc => dc.Id);

        builder.HasIndex(dc => new { dc.DealId, dc.ContactId }).IsUnique();

        builder.Property(dc => dc.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(dc => dc.UpdatedAt).IsRequired().HasColumnType("timestamptz");
    }
}
