using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class EmailSyncCheckpointConfiguration : IEntityTypeConfiguration<EmailSyncCheckpoint>
{
    public void Configure(EntityTypeBuilder<EmailSyncCheckpoint> builder)
    {
        builder.ToTable("EmailSyncCheckpoints");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.ConnectionId).IsUnique();

        builder.Property(c => c.ProviderCursor).HasMaxLength(500);
        builder.Property(c => c.IsRunning).IsRequired();
        builder.Property(c => c.RunStartedAt).HasColumnType("timestamptz");

        builder.Property(c => c.LastRunStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.LastRunAt).HasColumnType("timestamptz");
        builder.Property(c => c.LastError).HasMaxLength(2000);

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(c => c.Connection)
            .WithMany()
            .HasForeignKey(c => c.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
