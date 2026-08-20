using LimousineBooking.Application.Common;
using LimousineBooking.Application.Notifications;

namespace LimousineBooking.Application.Interfaces;

public interface IAdminNotificationService
{
    Task<PagedResult<FailedNotificationResponse>> GetFailedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Null if no notification exists with that id.</summary>
    Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default);
}
