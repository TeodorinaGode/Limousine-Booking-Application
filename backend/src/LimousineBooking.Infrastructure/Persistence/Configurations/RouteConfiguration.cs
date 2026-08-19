using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes", t => t.HasCheckConstraint(
            "CK_Routes_EstimatedDurationMinutes", "\"EstimatedDurationMinutes\" > 0"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.DepartureLocation)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Destination)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.EstimatedDurationMinutes)
            .IsRequired();

        builder.Property(r => r.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(r => r.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.HasIndex(r => r.IsActive);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasMany(r => r.Bookings)
            .WithOne(b => b.Route)
            .HasForeignKey(b => b.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data uses anonymous objects (matched to Route's properties by name)
        // rather than `new Route(...)`, since HasData snapshots are captured at
        // model-build time and must not depend on the entity's validating constructor.
        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DepartureLocation = "Basel",
                Destination = "Zurich",
                EstimatedDurationMinutes = 60,
                Price = 180.00m,
                Currency = "CHF",
                IsActive = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DepartureLocation = "Basel",
                Destination = "Bern",
                EstimatedDurationMinutes = 75,
                Price = 210.00m,
                Currency = "CHF",
                IsActive = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                DepartureLocation = "Zurich Airport",
                Destination = "Basel",
                EstimatedDurationMinutes = 70,
                Price = 250.00m,
                Currency = "CHF",
                IsActive = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            });
    }
}
