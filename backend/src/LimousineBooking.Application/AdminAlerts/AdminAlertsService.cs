using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.AdminAlerts;

public class AdminAlertsService : IAdminAlertsService
{
    private const int RecentItemsCount = 5;

    private readonly IBookingRepository _bookingRepository;
    private readonly ICrmEmailThreadRepository _threadRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AdminAlertsService(
        IBookingRepository bookingRepository,
        ICrmEmailThreadRepository threadRepository,
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _bookingRepository = bookingRepository;
        _threadRepository = threadRepository;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AdminAlertsSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        // Never having checked the bell before is treated as "everything is new" —
        // simpler and safer than guessing an arbitrary lookback window.
        var seenAt = user?.BookingsNotificationsSeenAt ?? DateTime.MinValue;

        var newBookingsCount = await _bookingRepository.CountCreatedAfterAsync(seenAt, cancellationToken);
        var recentBookings = await _bookingRepository.GetRecentlyCreatedAsync(RecentItemsCount, cancellationToken);

        var (unreadThreads, unreadTotal) = await _threadRepository.SearchAsync(
            new CrmEmailThreadSearchQuery { UnreadOnly = true, Page = 1, PageSize = RecentItemsCount },
            cancellationToken);

        return new AdminAlertsSummaryResponse
        {
            NewBookingsCount = newBookingsCount,
            RecentBookings = recentBookings.Select(b => new RecentBookingAlert
            {
                Id = b.Id,
                BookingReference = b.BookingReference,
                CustomerName = $"{b.CustomerFirstName} {b.CustomerLastName}",
                CreatedAt = b.CreatedAt,
                IsNew = b.CreatedAt > seenAt
            }).ToList(),
            UnreadEmailsCount = unreadTotal,
            RecentUnreadEmailThreads = unreadThreads.Select(t => new UnreadEmailThreadAlert
            {
                Id = t.Id,
                Subject = t.Subject,
                FromName = t.LatestFromName ?? t.LatestFromAddress,
                LastMessageAt = t.LastMessageAt
            }).ToList()
        };
    }

    public async Task MarkBookingsSeenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return;

        user.MarkBookingsNotificationsSeen(_dateTimeProvider.UtcNow);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
