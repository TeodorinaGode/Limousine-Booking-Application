using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmContactConfiguration : IEntityTypeConfiguration<CrmContact>
{
    public void Configure(EntityTypeBuilder<CrmContact> builder)
    {
        builder.ToTable("CrmContacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.JobTitle).HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.PreferredContactMethod).HasMaxLength(20);
        builder.Property(c => c.Notes).HasMaxLength(4000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.LastContactedAt).HasColumnType("timestamptz");
        builder.Property(c => c.NextFollowUpAt).HasColumnType("timestamptz");

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.IsArchived);
        builder.HasIndex(c => c.AssignedOwnerId);

        builder.HasOne(c => c.AssignedOwner)
            .WithMany()
            .HasForeignKey(c => c.AssignedOwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.CompanyContacts)
            .WithOne(cc => cc.Contact)
            .HasForeignKey(cc => cc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.DealContacts)
            .WithOne(dc => dc.Contact)
            .HasForeignKey(dc => dc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Activities)
            .WithOne(a => a.Contact)
            .HasForeignKey(a => a.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Tasks)
            .WithOne(t => t.Contact)
            .HasForeignKey(t => t.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
