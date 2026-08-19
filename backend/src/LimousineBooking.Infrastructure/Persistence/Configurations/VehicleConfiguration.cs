using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles", t => t.HasCheckConstraint(
            "CK_Vehicles_PassengerCapacity", "\"PassengerCapacity\" > 0"));

        builder.HasKey(v => v.Id);

        builder.Property(v => v.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(v => v.RegistrationNumber)
            .IsUnique();

        builder.Property(v => v.Make)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.VehicleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.PassengerCapacity)
            .IsRequired();

        builder.Property(v => v.IsActive)
            .IsRequired();

        builder.HasIndex(v => v.IsActive);

        builder.Property(v => v.Notes)
            .HasMaxLength(1000);

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasMany(v => v.Bookings)
            .WithOne(b => b.Vehicle)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
