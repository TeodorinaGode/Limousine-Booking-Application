using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmEmailTemplateRepository : ICrmEmailTemplateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmEmailTemplateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CrmEmailTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<CrmEmailTemplate> Items, int TotalCount)> SearchAsync(CrmEmailTemplateSearchQuery query, CancellationToken cancellationToken = default)
    {
        var templates = _dbContext.CrmEmailTemplates.AsQueryable();

        if (!query.IncludeArchived)
            templates = templates.Where(t => !t.IsArchived);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            templates = templates.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Subject, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
            templates = templates.Where(t => t.Category == query.Category);

        var totalCount = await templates.CountAsync(cancellationToken);

        var items = await templates
            .OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task AddAsync(CrmEmailTemplate template, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailTemplates.Add(template);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
