using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmActivityRepository
{
    Task<(IReadOnlyList<CrmActivity> Items, int TotalCount)> SearchAsync(CrmActivitySearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>Most recent activities for one company/contact/deal (whichever id is given), newest first.</summary>
    Task<IReadOnlyList<CrmActivity>> GetRecentForAsync(Guid? companyId, Guid? contactId, Guid? dealId, int take, CancellationToken cancellationToken = default);

    /// <summary>Most recent activities across every company/contact/deal, newest first — backs the CRM dashboard's activity feed.</summary>
    Task<IReadOnlyList<CrmActivity>> GetMostRecentAsync(int take, CancellationToken cancellationToken = default);

    Task AddAsync(CrmActivity activity, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
