using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class DriverAvailabilityConfiguration : IEntityTypeConfiguration<DriverAvailability>
{
    public void Configure(EntityTypeBuilder<DriverAvailability> builder)
    {
        builder.ToTable("DriverAvailabilities", t => t.HasCheckConstraint(
            "CK_DriverAvailabilities_EndAfterStart", "\"EndTime\" > \"StartTime\""));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(a => a.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(a => a.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(a => a.IsAvailable)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasIndex(a => a.DriverId);
        builder.HasIndex(a => new { a.DriverId, a.Date });
    }
}
