using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmEmailThreadRepository : ICrmEmailThreadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmEmailThreadRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CrmEmailThread?> GetByProviderThreadIdAsync(Guid connectionId, string providerThreadId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreads.SingleOrDefaultAsync(t => t.ConnectionId == connectionId && t.ProviderThreadId == providerThreadId, cancellationToken);

    public Task<CrmEmailThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreads.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<CrmEmailThread?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreads
            .Include(t => t.Messages).ThenInclude(m => m.Attachments)
            .Include(t => t.ThreadCompanies).ThenInclude(tc => tc.Company)
            .Include(t => t.ThreadContacts).ThenInclude(tc => tc.Contact)
            .Include(t => t.ThreadDeals).ThenInclude(td => td.Deal).ThenInclude(d => d!.Stage)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<CrmEmailThreadListItemResponse> Items, int TotalCount)> SearchAsync(CrmEmailThreadSearchQuery query, CancellationToken cancellationToken = default)
    {
        var threads = _dbContext.CrmEmailThreads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            threads = threads.Where(t =>
                EF.Functions.ILike(t.Subject, pattern) ||
                t.Messages.Any(m => EF.Functions.ILike(m.FromAddress, pattern) || (m.FromName != null && EF.Functions.ILike(m.FromName, pattern))));
        }

        if (query.CompanyId.HasValue)
            threads = threads.Where(t => t.ThreadCompanies.Any(tc => tc.CompanyId == query.CompanyId.Value));

        if (query.ContactId.HasValue)
            threads = threads.Where(t => t.ThreadContacts.Any(tc => tc.ContactId == query.ContactId.Value));

        if (query.DealId.HasValue)
            threads = threads.Where(t => t.ThreadDeals.Any(td => td.DealId == query.DealId.Value));

        if (query.UnreadOnly == true)
            threads = threads.Where(t => t.HasUnread);

        if (query.From.HasValue)
            threads = threads.Where(t => t.LastMessageAt >= query.From.Value);

        if (query.To.HasValue)
            threads = threads.Where(t => t.LastMessageAt <= query.To.Value);

        var totalCount = await threads.CountAsync(cancellationToken);

        var items = await threads
            .OrderByDescending(t => t.LastMessageAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new CrmEmailThreadListItemResponse
            {
                Id = t.Id,
                Subject = t.Subject,
                LastMessageAt = t.LastMessageAt,
                HasUnread = t.HasUnread,
                LatestFromAddress = t.Messages.OrderByDescending(m => m.OccurredAt).Select(m => m.FromAddress).FirstOrDefault(),
                LatestFromName = t.Messages.OrderByDescending(m => m.OccurredAt).Select(m => m.FromName).FirstOrDefault(),
                AssociatedCompanyNames = t.ThreadCompanies.Select(tc => tc.Company!.Name).ToList(),
                AssociatedContactNames = t.ThreadContacts.Select(tc => tc.Contact!.FirstName + " " + tc.Contact!.LastName).ToList(),
                AssociatedDealNames = t.ThreadDeals.Select(td => td.Deal!.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CrmEmailThreadCompany?> GetCompanyLinkAsync(Guid threadId, Guid companyId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreadCompanies.SingleOrDefaultAsync(l => l.ThreadId == threadId && l.CompanyId == companyId, cancellationToken);

    public Task<CrmEmailThreadContact?> GetContactLinkAsync(Guid threadId, Guid contactId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreadContacts.SingleOrDefaultAsync(l => l.ThreadId == threadId && l.ContactId == contactId, cancellationToken);

    public Task<CrmEmailThreadDeal?> GetDealLinkAsync(Guid threadId, Guid dealId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailThreadDeals.SingleOrDefaultAsync(l => l.ThreadId == threadId && l.DealId == dealId, cancellationToken);

    public Task AddAsync(CrmEmailThread thread, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreads.Add(thread);
        return Task.CompletedTask;
    }

    public Task AddCompanyLinkAsync(CrmEmailThreadCompany link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadCompanies.Add(link);
        return Task.CompletedTask;
    }

    public Task AddContactLinkAsync(CrmEmailThreadContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadContacts.Add(link);
        return Task.CompletedTask;
    }

    public Task AddDealLinkAsync(CrmEmailThreadDeal link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadDeals.Add(link);
        return Task.CompletedTask;
    }

    public Task RemoveCompanyLinkAsync(CrmEmailThreadCompany link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadCompanies.Remove(link);
        return Task.CompletedTask;
    }

    public Task RemoveContactLinkAsync(CrmEmailThreadContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadContacts.Remove(link);
        return Task.CompletedTask;
    }

    public Task RemoveDealLinkAsync(CrmEmailThreadDeal link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailThreadDeals.Remove(link);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
