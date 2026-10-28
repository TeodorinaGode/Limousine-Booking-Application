using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmActivityRepository : ICrmActivityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmActivityRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<CrmActivity> BaseQuery() =>
        _dbContext.CrmActivities
            .Include(a => a.User)
            .Include(a => a.Company)
            .Include(a => a.Contact)
            .Include(a => a.Deal)
            .AsQueryable();

    public async Task<(IReadOnlyList<CrmActivity> Items, int TotalCount)> SearchAsync(CrmActivitySearchQuery query, CancellationToken cancellationToken = default)
    {
        var activities = BaseQuery();

        if (Enum.TryParse<CrmActivityType>(query.ActivityType, true, out var activityType))
            activities = activities.Where(a => a.ActivityType == activityType);

        if (query.CompanyId.HasValue)
            activities = activities.Where(a => a.CompanyId == query.CompanyId.Value);

        if (query.ContactId.HasValue)
            activities = activities.Where(a => a.ContactId == query.ContactId.Value);

        if (query.DealId.HasValue)
            activities = activities.Where(a => a.DealId == query.DealId.Value);

        if (query.UserId.HasValue)
            activities = activities.Where(a => a.UserId == query.UserId.Value);

        if (query.From.HasValue)
            activities = activities.Where(a => a.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            activities = activities.Where(a => a.OccurredAt <= query.To.Value);

        var totalCount = await activities.CountAsync(cancellationToken);

        var items = await activities
            .OrderByDescending(a => a.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CrmActivity>> GetRecentForAsync(Guid? companyId, Guid? contactId, Guid? dealId, int take, CancellationToken cancellationToken = default)
    {
        var activities = BaseQuery().Where(a =>
            (companyId.HasValue && a.CompanyId == companyId.Value) ||
            (contactId.HasValue && a.ContactId == contactId.Value) ||
            (dealId.HasValue && a.DealId == dealId.Value));

        return await activities
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CrmActivity>> GetMostRecentAsync(int take, CancellationToken cancellationToken = default) =>
        await BaseQuery()
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task AddAsync(CrmActivity activity, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmActivities.Add(activity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
