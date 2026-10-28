using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmCompanyRepository
{
    /// <summary>Includes AssignedOwner only — for list/search results.</summary>
    Task<(IReadOnlyList<CrmCompany> Items, int TotalCount)> SearchAsync(CrmCompanySearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>No includes — for existence/validation checks (e.g. a deal referencing this company).</summary>
    Task<CrmCompany?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes AssignedOwner, linked contacts (+ their CrmContact), deals, tasks, and the most recent activities — for the detail page.</summary>
    Task<CrmCompany?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmCompanyContact?> GetCompanyContactLinkAsync(Guid companyId, Guid contactId, CancellationToken cancellationToken = default);

    Task AddAsync(CrmCompany company, CancellationToken cancellationToken = default);

    Task AddCompanyContactAsync(CrmCompanyContact link, CancellationToken cancellationToken = default);

    Task RemoveCompanyContactAsync(CrmCompanyContact link, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
