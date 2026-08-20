using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(l => l.Latitude)
            .IsRequired();

        builder.Property(l => l.Longitude)
            .IsRequired();

        builder.Property(l => l.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.Description)
            .HasMaxLength(300);

        builder.Property(l => l.IsActive)
            .IsRequired();

        builder.Property(l => l.SortOrder)
            .IsRequired();

        builder.HasIndex(l => l.IsActive);

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Real, publicly-known geographic reference points (Prompt 19, section
        // 2) — city-centre/airport coordinates, not business-invented facts.
        // Seed data uses anonymous objects (matched to Location's properties
        // by name) rather than `new Location(...)`, matching RouteConfiguration's
        // precedent: HasData snapshots are captured at model-build time and
        // must not depend on the entity's validating constructor.
        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            Seed("10000000-0000-0000-0000-000000000001", "Basel", "CH", 47.5596, 7.5886, LocationType.City, "Major Swiss city", 1, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000002", "Zurich", "CH", 47.3769, 8.5417, LocationType.City, "Major Swiss city", 2, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000003", "Bern", "CH", 46.9480, 7.4474, LocationType.City, "Swiss capital", 3, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000004", "Geneva", "CH", 46.2044, 6.1432, LocationType.City, "Major Swiss city", 4, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000005", "Zurich Airport", "CH", 47.4647, 8.5492, LocationType.Airport, "International airport", 5, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000006", "Basel Airport", "CH", 47.5896, 7.5299, LocationType.Airport, "International airport", 6, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000007", "Geneva Airport", "CH", 46.2381, 6.1089, LocationType.Airport, "International airport", 7, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000008", "Milan", "IT", 45.4642, 9.1900, LocationType.Destination, "Nearby European destination", 8, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000009", "Munich", "DE", 48.1351, 11.5820, LocationType.Destination, "Nearby European destination", 9, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000010", "Stuttgart", "DE", 48.7758, 9.1829, LocationType.Destination, "Nearby European destination", 10, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000011", "Frankfurt", "DE", 50.1109, 8.6821, LocationType.Destination, "Nearby European destination", 11, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000012", "Paris", "FR", 48.8566, 2.3522, LocationType.Destination, "Nearby European destination", 12, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000013", "Luxembourg", "LU", 49.6116, 6.1319, LocationType.Destination, "Nearby European destination", 13, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000014", "Brussels", "BE", 50.8503, 4.3517, LocationType.Destination, "Nearby European destination", 14, seedTimestamp),
            Seed("10000000-0000-0000-0000-000000000015", "Innsbruck", "AT", 47.2692, 11.4041, LocationType.Destination, "Nearby European destination", 15, seedTimestamp));
    }

    private static object Seed(string id, string name, string countryCode, double latitude, double longitude, LocationType type, string description, int sortOrder, DateTime seedTimestamp) =>
        new
        {
            Id = Guid.Parse(id),
            Name = name,
            CountryCode = countryCode,
            Latitude = latitude,
            Longitude = longitude,
            Type = type,
            Description = description,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = seedTimestamp,
            UpdatedAt = seedTimestamp
        };
}
