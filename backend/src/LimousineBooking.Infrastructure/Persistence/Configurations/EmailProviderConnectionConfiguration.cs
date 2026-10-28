using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class EmailProviderConnectionConfiguration : IEntityTypeConfiguration<EmailProviderConnection>
{
    public void Configure(EntityTypeBuilder<EmailProviderConnection> builder)
    {
        builder.ToTable("EmailProviderConnections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.EmailAddress).IsRequired().HasMaxLength(200);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Encrypted via ASP.NET Core Data Protection before it ever reaches this
        // entity — the column holds ciphertext, never a usable refresh token.
        builder.Property(c => c.EncryptedRefreshToken).HasMaxLength(4000);
        builder.Property(c => c.AccessToken).HasMaxLength(4000);
        builder.Property(c => c.AccessTokenExpiresAt).HasColumnType("timestamptz");
        builder.Property(c => c.LastSyncedAt).HasColumnType("timestamptz");
        builder.Property(c => c.LastSyncError).HasMaxLength(2000);

        builder.Property(c => c.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(c => c.Status);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
