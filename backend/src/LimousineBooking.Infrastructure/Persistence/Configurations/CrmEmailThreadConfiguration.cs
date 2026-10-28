using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailThreadConfiguration : IEntityTypeConfiguration<CrmEmailThread>
{
    public void Configure(EntityTypeBuilder<CrmEmailThread> builder)
    {
        builder.ToTable("CrmEmailThreads");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ProviderThreadId).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
        builder.Property(t => t.LastMessageAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(t => t.HasUnread).IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(t => new { t.ConnectionId, t.ProviderThreadId }).IsUnique();
        builder.HasIndex(t => t.LastMessageAt);

        builder.HasOne<EmailProviderConnection>()
            .WithMany()
            .HasForeignKey(t => t.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Messages)
            .WithOne(m => m.Thread)
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ThreadCompanies)
            .WithOne(tc => tc.Thread)
            .HasForeignKey(tc => tc.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ThreadContacts)
            .WithOne(tc => tc.Thread)
            .HasForeignKey(tc => tc.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ThreadDeals)
            .WithOne(td => td.Thread)
            .HasForeignKey(td => td.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
