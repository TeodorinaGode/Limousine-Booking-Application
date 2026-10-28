using LimousineBooking.Application.Email;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmEmailThreadRepository
{
    Task<CrmEmailThread?> GetByProviderThreadIdAsync(Guid connectionId, string providerThreadId, CancellationToken cancellationToken = default);

    Task<CrmEmailThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes Messages (with their User/Company/Contact/Deal not needed) and the three association join collections — for the conversation detail view.</summary>
    Task<CrmEmailThread?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CrmEmailThreadListItemResponse> Items, int TotalCount)> SearchAsync(CrmEmailThreadSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmEmailThreadCompany?> GetCompanyLinkAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default);

    Task<CrmEmailThreadContact?> GetContactLinkAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default);

    Task<CrmEmailThreadDeal?> GetDealLinkAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default);

    Task AddAsync(CrmEmailThread thread, CancellationToken cancellationToken = default);

    Task AddCompanyLinkAsync(CrmEmailThreadCompany link, CancellationToken cancellationToken = default);

    Task AddContactLinkAsync(CrmEmailThreadContact link, CancellationToken cancellationToken = default);

    Task AddDealLinkAsync(CrmEmailThreadDeal link, CancellationToken cancellationToken = default);

    Task RemoveCompanyLinkAsync(CrmEmailThreadCompany link, CancellationToken cancellationToken = default);

    Task RemoveContactLinkAsync(CrmEmailThreadContact link, CancellationToken cancellationToken = default);

    Task RemoveDealLinkAsync(CrmEmailThreadDeal link, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
