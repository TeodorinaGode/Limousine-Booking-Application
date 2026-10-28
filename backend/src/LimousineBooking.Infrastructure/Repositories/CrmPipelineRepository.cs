using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmPipelineRepository : ICrmPipelineRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmPipelineRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CrmPipeline?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _dbContext.CrmPipelines
            .Include(p => p.Stages.OrderBy(s => s.SortOrder))
            .Where(p => p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CrmPipelineStage?> GetStageByIdAsync(Guid stageId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmPipelineStages.SingleOrDefaultAsync(s => s.Id == stageId, cancellationToken);
}
