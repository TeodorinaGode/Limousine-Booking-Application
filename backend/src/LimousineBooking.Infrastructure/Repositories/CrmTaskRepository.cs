using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmTaskRepository : ICrmTaskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmTaskRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<CrmTask> BaseQuery() =>
        _dbContext.CrmTasks
            .Include(t => t.AssignedUser)
            .Include(t => t.Company)
            .Include(t => t.Contact)
            .Include(t => t.Deal)
            .AsQueryable();

    public async Task<(IReadOnlyList<CrmTask> Items, int TotalCount)> SearchAsync(CrmTaskSearchQuery query, CancellationToken cancellationToken = default)
    {
        var tasks = BaseQuery();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            tasks = tasks.Where(t => EF.Functions.ILike(t.Title, pattern));
        }

        if (Enum.TryParse<CrmTaskStatus>(query.Status, true, out var status))
            tasks = tasks.Where(t => t.Status == status);

        if (Enum.TryParse<CrmPriority>(query.Priority, true, out var priority))
            tasks = tasks.Where(t => t.Priority == priority);

        if (query.AssignedUserId.HasValue)
            tasks = tasks.Where(t => t.AssignedUserId == query.AssignedUserId.Value);

        if (query.CompanyId.HasValue)
            tasks = tasks.Where(t => t.CompanyId == query.CompanyId.Value);

        if (query.ContactId.HasValue)
            tasks = tasks.Where(t => t.ContactId == query.ContactId.Value);

        if (query.DealId.HasValue)
            tasks = tasks.Where(t => t.DealId == query.DealId.Value);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (query.Overdue == true)
        {
            tasks = tasks.Where(t =>
                t.DueDate < today &&
                t.Status != CrmTaskStatus.Completed && t.Status != CrmTaskStatus.Cancelled);
        }

        if (query.DueToday == true)
            tasks = tasks.Where(t => t.DueDate == today);

        var totalCount = await tasks.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        tasks = query.SortBy?.ToLowerInvariant() switch
        {
            "priority" => descending ? tasks.OrderByDescending(t => t.Priority) : tasks.OrderBy(t => t.Priority),
            "createdat" => descending ? tasks.OrderByDescending(t => t.CreatedAt) : tasks.OrderBy(t => t.CreatedAt),
            _ => descending ? tasks.OrderByDescending(t => t.DueDate) : tasks.OrderBy(t => t.DueDate)
        };

        var items = await tasks
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CrmTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        BaseQuery().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CrmTask>> GetOpenForAsync(Guid? companyId, Guid? contactId, Guid? dealId, CancellationToken cancellationToken = default)
    {
        var tasks = BaseQuery().Where(t =>
            t.Status != CrmTaskStatus.Completed && t.Status != CrmTaskStatus.Cancelled &&
            ((companyId.HasValue && t.CompanyId == companyId.Value) ||
             (contactId.HasValue && t.ContactId == contactId.Value) ||
             (dealId.HasValue && t.DealId == dealId.Value)));

        return await tasks.OrderBy(t => t.DueDate).ToListAsync(cancellationToken);
    }

    public Task AddAsync(CrmTask task, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
