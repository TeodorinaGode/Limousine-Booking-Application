using LimousineBooking.Application.Email;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmEmailTemplateRepository
{
    Task<CrmEmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CrmEmailTemplate> Items, int TotalCount)> SearchAsync(CrmEmailTemplateSearchQuery query, CancellationToken cancellationToken = default);

    Task AddAsync(CrmEmailTemplate template, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
