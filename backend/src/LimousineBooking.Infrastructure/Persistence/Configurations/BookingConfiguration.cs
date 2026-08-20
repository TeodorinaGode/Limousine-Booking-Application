using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", t => t.HasCheckConstraint(
            "CK_Bookings_PassengerCount", "\"PassengerCount\" > 0"));

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(b => b.BookingReference)
            .IsUnique();

        builder.Property(b => b.CustomerFirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.CustomerLastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.CustomerEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.CustomerPhone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(b => b.TravelDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(b => b.PickupTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(b => b.PickupAddress)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.PassengerCount)
            .IsRequired();

        builder.Property(b => b.Notes)
            .HasMaxLength(1000);

        builder.Property(b => b.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(b => b.RequiresManualAssignment)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.ManualAssignmentReason)
            .HasMaxLength(500);

        builder.Property(b => b.AssignmentType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.CancelledAt)
            .HasColumnType("timestamptz");

        builder.Property(b => b.RideStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.RideStatus.Upcoming);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(b => b.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasIndex(b => b.TravelDate);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.RideStatus);
        builder.HasIndex(b => b.DriverId);
        builder.HasIndex(b => b.RouteId);
        builder.HasIndex(b => b.CreatedAt);

        // Supports looking up a driver's schedule for a given day, and the
        // automatic-assignment conflict scan (candidate driver IDs + travel date).
        builder.HasIndex(b => new { b.DriverId, b.TravelDate });

        // Same purpose as the DriverId+TravelDate index above, but for the
        // explicit vehicle-conflict check the assignment service also runs
        // (section 14 — a vehicle could in principle be reassigned to a
        // different driver between bookings, so the driver-scoped index alone
        // isn't sufficient to answer "is this vehicle already booked then?").
        builder.HasIndex(b => new { b.VehicleId, b.TravelDate });

        // Lets the future admin UI query "which bookings need manual assignment"
        // without scanning every Pending booking.
        builder.HasIndex(b => b.RequiresManualAssignment);

        // Supports the admin booking search's "customer email" filter/search field.
        builder.HasIndex(b => b.CustomerEmail);

        builder.HasOne(b => b.Route)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Driver)
            .WithMany(d => d.Bookings)
            .HasForeignKey(b => b.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Vehicle)
            .WithMany(v => v.Bookings)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.StatusHistory)
            .WithOne(h => h.Booking)
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Notifications)
            .WithOne(n => n.Booking)
            .HasForeignKey(n => n.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
