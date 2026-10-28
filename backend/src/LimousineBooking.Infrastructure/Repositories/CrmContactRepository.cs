using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmContactRepository : ICrmContactRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmContactRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<CrmContact> Items, int TotalCount)> SearchAsync(CrmContactSearchQuery query, CancellationToken cancellationToken = default)
    {
        var contacts = _dbContext.CrmContacts
            .Include(c => c.AssignedOwner)
            .AsQueryable();

        if (!query.IncludeArchived)
            contacts = contacts.Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            contacts = contacts.Where(c =>
                EF.Functions.ILike(c.FirstName, pattern) ||
                EF.Functions.ILike(c.LastName, pattern) ||
                EF.Functions.ILike(c.Email, pattern) ||
                (c.JobTitle != null && EF.Functions.ILike(c.JobTitle, pattern)));
        }

        if (Enum.TryParse<CrmLifecycleStatus>(query.Status, true, out var status))
            contacts = contacts.Where(c => c.Status == status);

        if (query.AssignedOwnerId.HasValue)
            contacts = contacts.Where(c => c.AssignedOwnerId == query.AssignedOwnerId.Value);

        if (query.CompanyId.HasValue)
            contacts = contacts.Where(c => c.CompanyContacts.Any(cc => cc.CompanyId == query.CompanyId.Value));

        if (query.NeedsFollowUp == true)
        {
            var now = DateTime.UtcNow;
            contacts = contacts.Where(c => c.NextFollowUpAt != null && c.NextFollowUpAt <= now);
        }

        var totalCount = await contacts.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        contacts = query.SortBy?.ToLowerInvariant() switch
        {
            "lastname" => descending ? contacts.OrderByDescending(c => c.LastName) : contacts.OrderBy(c => c.LastName),
            "email" => descending ? contacts.OrderByDescending(c => c.Email) : contacts.OrderBy(c => c.Email),
            "createdat" => descending ? contacts.OrderByDescending(c => c.CreatedAt) : contacts.OrderBy(c => c.CreatedAt),
            "nextfollowupat" => descending ? contacts.OrderByDescending(c => c.NextFollowUpAt) : contacts.OrderBy(c => c.NextFollowUpAt),
            _ => descending ? contacts.OrderByDescending(c => c.FirstName) : contacts.OrderBy(c => c.FirstName)
        };

        var items = await contacts
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CrmContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmContacts
            .Include(c => c.AssignedOwner)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CrmContact?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmContacts
            .Include(c => c.AssignedOwner)
            .Include(c => c.CompanyContacts).ThenInclude(cc => cc.Company)
            .Include(c => c.DealContacts).ThenInclude(dc => dc.Deal).ThenInclude(d => d!.Stage)
            .Include(c => c.Tasks.Where(t => t.Status == CrmTaskStatus.ToDo || t.Status == CrmTaskStatus.InProgress))
                .ThenInclude(t => t.AssignedUser)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> HasDuplicateEmailAsync(string email, Guid? excludeContactId, CancellationToken cancellationToken = default)
    {
        var contactsQuery = _dbContext.CrmContacts.Where(c => EF.Functions.ILike(c.Email, email));

        if (excludeContactId.HasValue)
            contactsQuery = contactsQuery.Where(c => c.Id != excludeContactId.Value);

        return await contactsQuery.AnyAsync(cancellationToken);
    }

    public Task<CrmContact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.CrmContacts
            .Include(c => c.CompanyContacts).ThenInclude(cc => cc.Company)
            .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Email, email), cancellationToken);

    public Task AddAsync(CrmContact contact, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmContacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
