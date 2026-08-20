using LimousineBooking.Application.Notifications;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Persistence for Notification rows, which double as this application's transactional outbox — see Notification's summary.</summary>
public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Messages ready to (re)send: Pending with no NextAttemptAt or one that has
    /// passed, plus Processing messages stuck since before <paramref name="staleProcessingBefore"/>
    /// (crash recovery — the process that claimed them never finished).
    /// </summary>
    Task<IReadOnlyList<Notification>> GetDueForProcessingAsync(
        DateTime now, DateTime staleProcessingBefore, int batchSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Notification> Items, int TotalCount)> SearchFailedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<OutboxSummaryCounts> GetSummaryAsync(DateTime today, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
