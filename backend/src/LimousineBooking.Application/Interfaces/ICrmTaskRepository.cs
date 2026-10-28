using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmTaskRepository
{
    Task<(IReadOnlyList<CrmTask> Items, int TotalCount)> SearchAsync(CrmTaskSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Open (ToDo/InProgress) tasks for one company/contact/deal, soonest due first.</summary>
    Task<IReadOnlyList<CrmTask>> GetOpenForAsync(Guid? companyId, Guid? contactId, Guid? dealId, CancellationToken cancellationToken = default);

    Task AddAsync(CrmTask task, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
