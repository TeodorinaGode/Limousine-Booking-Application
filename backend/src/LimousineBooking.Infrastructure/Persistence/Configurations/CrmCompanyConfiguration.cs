using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmCompanyConfiguration : IEntityTypeConfiguration<CrmCompany>
{
    public void Configure(EntityTypeBuilder<CrmCompany> builder)
    {
        builder.ToTable("CrmCompanies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CompanyType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.Website).HasMaxLength(300);
        builder.Property(c => c.Domain).HasMaxLength(200);
        builder.Property(c => c.Industry).HasMaxLength(100);
        builder.Property(c => c.Country).HasMaxLength(100);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Address).HasMaxLength(300);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.LeadSource).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(4000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.LastContactedAt).HasColumnType("timestamptz");
        builder.Property(c => c.NextFollowUpAt).HasColumnType("timestamptz");

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CompanyType);
        builder.HasIndex(c => c.IsArchived);
        builder.HasIndex(c => c.AssignedOwnerId);
        builder.HasIndex(c => c.NextFollowUpAt);

        builder.HasOne(c => c.AssignedOwner)
            .WithMany()
            .HasForeignKey(c => c.AssignedOwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.CompanyContacts)
            .WithOne(cc => cc.Company)
            .HasForeignKey(cc => cc.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Deals)
            .WithOne(d => d.Company)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Activities)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Tasks)
            .WithOne(t => t.Company)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
