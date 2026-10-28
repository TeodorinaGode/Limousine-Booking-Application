using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmPipelineConfiguration : IEntityTypeConfiguration<CrmPipeline>
{
    public void Configure(EntityTypeBuilder<CrmPipeline> builder)
    {
        builder.ToTable("CrmPipelines");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.IsDefault).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasMany(p => p.Stages)
            .WithOne(s => s.Pipeline)
            .HasForeignKey(s => s.PipelineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
