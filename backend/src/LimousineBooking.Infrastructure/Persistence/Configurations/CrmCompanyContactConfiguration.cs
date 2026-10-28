using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmCompanyContactConfiguration : IEntityTypeConfiguration<CrmCompanyContact>
{
    public void Configure(EntityTypeBuilder<CrmCompanyContact> builder)
    {
        builder.ToTable("CrmCompanyContacts");

        builder.HasKey(cc => cc.Id);

        builder.HasIndex(cc => new { cc.CompanyId, cc.ContactId }).IsUnique();

        builder.Property(cc => cc.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(cc => cc.UpdatedAt).IsRequired().HasColumnType("timestamptz");
    }
}
