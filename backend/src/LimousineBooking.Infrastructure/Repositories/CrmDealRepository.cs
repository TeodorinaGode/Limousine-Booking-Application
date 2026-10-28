using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmDealRepository : ICrmDealRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmDealRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<CrmDeal> BaseQuery() =>
        _dbContext.CrmDeals
            .Include(d => d.Company)
            .Include(d => d.Stage)
            .Include(d => d.Owner)
            .AsQueryable();

    public async Task<(IReadOnlyList<CrmDeal> Items, int TotalCount)> SearchAsync(CrmDealSearchQuery query, CancellationToken cancellationToken = default)
    {
        var deals = BaseQuery();

        if (!query.IncludeArchived)
            deals = deals.Where(d => !d.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            deals = deals.Where(d => EF.Functions.ILike(d.Name, pattern));
        }

        if (query.StageId.HasValue)
            deals = deals.Where(d => d.StageId == query.StageId.Value);

        if (query.CompanyId.HasValue)
            deals = deals.Where(d => d.CompanyId == query.CompanyId.Value);

        if (query.OwnerId.HasValue)
            deals = deals.Where(d => d.OwnerId == query.OwnerId.Value);

        if (query.IsOpen.HasValue)
        {
            deals = query.IsOpen.Value
                ? deals.Where(d => !d.Stage!.IsWon && !d.Stage!.IsLost)
                : deals.Where(d => d.Stage!.IsWon || d.Stage!.IsLost);
        }

        var totalCount = await deals.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        deals = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? deals.OrderByDescending(d => d.Name) : deals.OrderBy(d => d.Name),
            "value" => descending ? deals.OrderByDescending(d => d.Value) : deals.OrderBy(d => d.Value),
            "probability" => descending ? deals.OrderByDescending(d => d.Probability) : deals.OrderBy(d => d.Probability),
            "expectedclosedate" => descending ? deals.OrderByDescending(d => d.ExpectedCloseDate) : deals.OrderBy(d => d.ExpectedCloseDate),
            _ => descending ? deals.OrderByDescending(d => d.CreatedAt) : deals.OrderBy(d => d.CreatedAt)
        };

        var items = await deals
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CrmDeal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        BaseQuery().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<CrmDeal?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        BaseQuery()
            .Include(d => d.DealContacts).ThenInclude(dc => dc.Contact)
            .Include(d => d.Tasks.Where(t => t.Status == Domain.Enums.CrmTaskStatus.ToDo || t.Status == Domain.Enums.CrmTaskStatus.InProgress))
                .ThenInclude(t => t.AssignedUser)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CrmDeal>> GetPipelineBoardAsync(Guid pipelineId, CancellationToken cancellationToken = default) =>
        await BaseQuery()
            .Where(d => d.PipelineId == pipelineId && !d.IsArchived)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<CrmDealContact?> GetDealContactLinkAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmDealContacts.SingleOrDefaultAsync(dc => dc.DealId == dealId && dc.ContactId == contactId, cancellationToken);

    public Task AddAsync(CrmDeal deal, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmDeals.Add(deal);
        return Task.CompletedTask;
    }

    public Task AddDealContactAsync(CrmDealContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmDealContacts.Add(link);
        return Task.CompletedTask;
    }

    public Task RemoveDealContactAsync(CrmDealContact link, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmDealContacts.Remove(link);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
