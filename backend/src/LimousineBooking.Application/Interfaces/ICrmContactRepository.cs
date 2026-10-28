using LimousineBooking.Application.Crm;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmContactRepository
{
    Task<(IReadOnlyList<CrmContact> Items, int TotalCount)> SearchAsync(CrmContactSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes AssignedOwner, linked companies, deals, tasks, and the most recent activities — for the detail page.</summary>
    Task<CrmContact?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasDuplicateEmailAsync(string email, Guid? excludeContactId, CancellationToken cancellationToken = default);

    /// <summary>Exact (case-insensitive) email match — backs the email sync processor's auto-association of a new thread with a known contact. Includes linked companies.</summary>
    Task<CrmContact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(CrmContact contact, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
