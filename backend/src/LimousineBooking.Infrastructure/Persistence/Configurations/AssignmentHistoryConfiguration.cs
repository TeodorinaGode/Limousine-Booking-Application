using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class AssignmentHistoryConfiguration : IEntityTypeConfiguration<AssignmentHistory>
{
    public void Configure(EntityTypeBuilder<AssignmentHistory> builder)
    {
        builder.ToTable("AssignmentHistories");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignmentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.AssignedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(a => a.Reason)
            .HasMaxLength(500);

        // Supports "show assignment history for this booking, most recent first".
        builder.HasIndex(a => new { a.BookingId, a.AssignedAt });

        builder.HasOne(a => a.Booking)
            .WithMany()
            .HasForeignKey(a => a.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
