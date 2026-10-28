using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmCompanyRepository : ICrmCompanyRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmCompanyRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<CrmCompany> Items, int TotalCount)> SearchAsync(CrmCompanySearchQuery query, CancellationToken cancellationToken = default)
    {
        var companies = _dbContext.CrmCompanies
            .Include(c => c.AssignedOwner)
            .AsQueryable();

        if (!query.IncludeArchived)
            companies = companies.Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            companies = companies.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Domain != null && EF.Functions.ILike(c.Domain, pattern)) ||
                (c.Email != null && EF.Functions.ILike(c.Email, pattern)));
        }

        if (Enum.TryParse<CrmCompanyType>(query.CompanyType, true, out var companyType))
            companies = companies.Where(c => c.CompanyType == companyType);

        if (Enum.TryParse<CrmLifecycleStatus>(query.Status, true, out var status))
            companies = companies.Where(c => c.Status == status);

        if (Enum.TryParse<CrmPriority>(query.Priority, true, out var priority))
            companies = companies.Where(c => c.Priority == priority);

        if (query.AssignedOwnerId.HasValue)
            companies = companies.Where(c => c.AssignedOwnerId == query.AssignedOwnerId.Value);

        if (query.NeedsFollowUp == true)
        {
            var now = DateTime.UtcNow;
            companies = companies.Where(c => c.NextFollowUpAt != null && c.NextFollowUpAt <= now);
        }

        var totalCount = await companies.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        companies = query.SortBy?.ToLowerInvariant() switch
        {
            "status" => descending ? companies.OrderByDescending(c => c.Status) : companies.OrderBy(c => c.Status),
            "priority" => descending ? companies.OrderByDescending(c => c.Priority) : companies.OrderBy(c => c.Priority),
            "createdat" => descending ? companies.OrderByDescending(c => c.CreatedAt) : companies.OrderBy(c => c.CreatedAt),
            "nextfollowupat" => descending ? companies.OrderByDescending(c => c.NextFollowUpAt) : companies.OrderBy(c => c.NextFollowUpAt),
            // "name" and any unrecognized value fall back to the default —
            // sortBy is never used to build raw SQL, only to pick a known column.
            _ => descending ? companies.OrderByDescending(c => c.Name) : companies.OrderBy(c => c.Name)
        };

        var items = await companies
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CrmCompany?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmCompanies
            .Include(c => c.AssignedOwner)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CrmCompany?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmCompanies
            .Include(c => c.AssignedOwner)
            .Include(c => c.CompanyContacts).ThenInclude(cc => cc.Contact)
            .Include(c => c.Deals).ThenInclude(d => d.Stage)
            .Include(c => c.Tasks.Where(t => t.Status == Domain.Enums.CrmTaskStatus.ToDo || t.Status == Domain.Enums.CrmTaskStatus.InProgress))
                .ThenInclude(t => t.AssignedUser)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CrmCompanyContact?> GetCompanyContactLinkAsync(Guid companyId, Guid contactId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmCompanyContacts.SingleOrDefaultAsync(cc => cc.CompanyId == companyId && cc.ContactId == contactId, cancellationToken);

    public Task AddAsync(CrmCompany company, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmCompanies.Add(company);
        return Task.CompletedTask;
    }

    public Task AddCompanyContactAsync(CrmCompanyContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmCompanyContacts.Add(link);
        return Task.CompletedTask;
    }

    public Task RemoveCompanyContactAsync(CrmCompanyContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmCompanyContacts.Remove(link);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
