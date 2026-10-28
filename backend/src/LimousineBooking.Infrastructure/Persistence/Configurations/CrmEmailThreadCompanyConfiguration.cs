using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailThreadCompanyConfiguration : IEntityTypeConfiguration<CrmEmailThreadCompany>
{
    public void Configure(EntityTypeBuilder<CrmEmailThreadCompany> builder)
    {
        builder.ToTable("CrmEmailThreadCompanies");

        builder.HasKey(tc => tc.Id);

        builder.HasIndex(tc => new { tc.ThreadId, tc.CompanyId }).IsUnique();

        builder.Property(tc => tc.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(tc => tc.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(tc => tc.Company)
            .WithMany()
            .HasForeignKey(tc => tc.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
