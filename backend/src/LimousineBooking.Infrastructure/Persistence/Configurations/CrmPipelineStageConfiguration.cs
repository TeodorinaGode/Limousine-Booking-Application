using LimousineBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LimousineBooking.Infrastructure.Persistence.Configurations;

public class CrmPipelineStageConfiguration : IEntityTypeConfiguration<CrmPipelineStage>
{
    public void Configure(EntityTypeBuilder<CrmPipelineStage> builder)
    {
        builder.ToTable("CrmPipelineStages");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.SortOrder).IsRequired();
        builder.Property(s => s.IsWon).IsRequired();
        builder.Property(s => s.IsLost).IsRequired();
        builder.Property(s => s.DefaultProbability).IsRequired();

        builder.Property(s => s.CreatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.PipelineId, s.SortOrder });
    }
}
