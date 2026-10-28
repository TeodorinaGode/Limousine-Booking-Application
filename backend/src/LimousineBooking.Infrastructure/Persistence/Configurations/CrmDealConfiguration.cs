using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmDealConfiguration : IEntityTypeConfiguration<CrmDeal>
{
    public void Configure(EntityTypeBuilder<CrmDeal> builder)
    {
        builder.ToTable("CrmDeals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(4000);
        builder.Property(d => d.DealType).HasMaxLength(100);
        builder.Property(d => d.Value).IsRequired().HasColumnType("numeric(12,2)");
        builder.Property(d => d.Currency).IsRequired().HasMaxLength(3);
        builder.Property(d => d.Probability).IsRequired();
        builder.Property(d => d.NextAction).HasMaxLength(500);
        builder.Property(d => d.LossReason).HasMaxLength(500);
        builder.Property(d => d.Notes).HasMaxLength(4000);
        builder.Property(d => d.ClosedAt).HasColumnType("timestamptz");

        builder.Property(d => d.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(d => d.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(d => d.CompanyId);
        builder.HasIndex(d => d.StageId);
        builder.HasIndex(d => d.IsArchived);
        builder.HasIndex(d => d.OwnerId);
        builder.HasIndex(d => d.ExpectedCloseDate);

        builder.HasOne(d => d.Company)
            .WithMany(c => c.Deals)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Pipeline)
            .WithMany()
            .HasForeignKey(d => d.PipelineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Stage)
            .WithMany()
            .HasForeignKey(d => d.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.DealContacts)
            .WithOne(dc => dc.Deal)
            .HasForeignKey(dc => dc.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Activities)
            .WithOne(a => a.Deal)
            .HasForeignKey(a => a.DealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Tasks)
            .WithOne(t => t.Deal)
            .HasForeignKey(t => t.DealId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
