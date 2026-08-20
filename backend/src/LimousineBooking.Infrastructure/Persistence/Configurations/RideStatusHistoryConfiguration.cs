using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class RideStatusHistoryConfiguration : IEntityTypeConfiguration<RideStatusHistory>
{
    public void Configure(EntityTypeBuilder<RideStatusHistory> builder)
    {
        builder.ToTable("RideStatusHistories");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.ChangedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Supports "show ride-status history for this booking, most recent first".
        builder.HasIndex(r => new { r.BookingId, r.ChangedAt });

        builder.HasOne(r => r.Booking)
            .WithMany(b => b.RideStatusHistory)
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
