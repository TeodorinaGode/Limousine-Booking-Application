using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmTaskConfiguration : IEntityTypeConfiguration<CrmTask>
{
    public void Configure(EntityTypeBuilder<CrmTask> builder)
    {
        builder.ToTable("CrmTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.DueDate).IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CompletedAt).HasColumnType("timestamptz");

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedUserId);
        builder.HasIndex(t => t.CompanyId);
        builder.HasIndex(t => t.ContactId);
        builder.HasIndex(t => t.DealId);

        builder.HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
