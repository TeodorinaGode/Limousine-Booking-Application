using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmActivityConfiguration : IEntityTypeConfiguration<CrmActivity>
{
    public void Configure(EntityTypeBuilder<CrmActivity> builder)
    {
        builder.ToTable("CrmActivities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActivityType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Subject).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Description).HasMaxLength(4000);
        builder.Property(a => a.Outcome).HasMaxLength(1000);
        builder.Property(a => a.OccurredAt).IsRequired().HasColumnType("timestamptz");

        builder.Property(a => a.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.ContactId);
        builder.HasIndex(a => a.DealId);
        builder.HasIndex(a => a.UserId);

        // Stage 2A addition — at most one activity row per email message, which
        // is what prevents a sent-then-synced-back message from ever producing
        // two timeline entries.
        builder.HasIndex(a => a.EmailMessageId).IsUnique().HasFilter("\"EmailMessageId\" IS NOT NULL");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CrmEmailMessage>()
            .WithMany()
            .HasForeignKey(a => a.EmailMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
