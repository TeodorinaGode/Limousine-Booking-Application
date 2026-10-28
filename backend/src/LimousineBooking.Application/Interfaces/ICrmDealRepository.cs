using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmDealRepository
{
    Task<(IReadOnlyList<CrmDeal> Items, int TotalCount)> SearchAsync(CrmDealSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmDeal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes Company, Pipeline/Stage, Owner, linked contacts, tasks, and the most recent activities — for the detail page.</summary>
    Task<CrmDeal?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>All non-archived deals in the pipeline, grouped by stage — one query for the Kanban board.</summary>
    Task<IReadOnlyList<CrmDeal>> GetPipelineBoardAsync(Guid pipelineId, CancellationToken cancellationToken = default);

    Task<CrmDealContact?> GetDealContactLinkAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default);

    Task AddAsync(CrmDeal deal, CancellationToken cancellationToken = default);

    Task AddDealContactAsync(CrmDealContact link, CancellationToken cancellationToken = default);

    Task RemoveDealContactAsync(CrmDealContact link, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
