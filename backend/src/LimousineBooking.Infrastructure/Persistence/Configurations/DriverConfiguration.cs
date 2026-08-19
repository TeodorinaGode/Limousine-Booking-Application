using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(d => d.Id);

        builder.HasIndex(d => d.UserId)
            .IsUnique();

        builder.Property(d => d.Phone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.IsAvailable)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired();

        builder.HasIndex(d => d.IsAvailable);
        builder.HasIndex(d => d.IsActive);

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(d => d.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasOne(d => d.CurrentVehicle)
            .WithMany()
            .HasForeignKey(d => d.CurrentVehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Bookings)
            .WithOne(b => b.Driver)
            .HasForeignKey(b => b.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Availabilities)
            .WithOne(a => a.Driver)
            .HasForeignKey(a => a.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
