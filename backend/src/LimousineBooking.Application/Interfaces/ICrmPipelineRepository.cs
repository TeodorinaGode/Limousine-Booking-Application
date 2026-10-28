using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmPipelineRepository
{
    /// <summary>Includes Stages, ordered by SortOrder. Stage 1 ships exactly one default pipeline, seeded by migration.</summary>
    Task<CrmPipeline?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<CrmPipelineStage?> GetStageByIdAsync(Guid stageId, CancellationToken cancellationToken = default);
}
