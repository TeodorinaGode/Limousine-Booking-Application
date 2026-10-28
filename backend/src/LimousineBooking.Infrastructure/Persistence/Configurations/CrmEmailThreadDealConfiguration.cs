using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmEmailThreadDealConfiguration : IEntityTypeConfiguration<CrmEmailThreadDeal>
{
    public void Configure(EntityTypeBuilder<CrmEmailThreadDeal> builder)
    {
        builder.ToTable("CrmEmailThreadDeals");

        builder.HasKey(td => td.Id);

        builder.HasIndex(td => new { td.ThreadId, td.DealId }).IsUnique();

        builder.Property(td => td.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(td => td.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(td => td.Deal)
            .WithMany()
            .HasForeignKey(td => td.DealId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
