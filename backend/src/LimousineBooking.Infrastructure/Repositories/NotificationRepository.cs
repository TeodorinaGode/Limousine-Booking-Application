using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Notifications.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetDueForProcessingAsync(
        DateTime now, DateTime staleProcessingBefore, int batchSize, CancellationToken cancellationToken = default) =>
        await _dbContext.Notifications
            .Where(n =>
                (n.Status == NotificationStatus.Pending && (n.NextAttemptAt == null || n.NextAttemptAt <= now)) ||
                (n.Status == NotificationStatus.Processing && n.ProcessingStartedAt != null && n.ProcessingStartedAt < staleProcessingBefore))
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> SearchFailedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Failed)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<OutboxSummaryCounts> GetSummaryAsync(DateTime today, CancellationToken cancellationToken = default) => new()
    {
        Pending = await _dbContext.Notifications.CountAsync(n => n.Status == NotificationStatus.Pending && n.RetryCount == 0, cancellationToken),
        Retrying = await _dbContext.Notifications.CountAsync(n => n.Status == NotificationStatus.Pending && n.RetryCount > 0, cancellationToken),
        Failed = await _dbContext.Notifications.CountAsync(n => n.Status == NotificationStatus.Failed, cancellationToken),
        SentToday = await _dbContext.Notifications.CountAsync(n => n.Status == NotificationStatus.Sent && n.SentAt != null && n.SentAt >= today, cancellationToken)
    };

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
