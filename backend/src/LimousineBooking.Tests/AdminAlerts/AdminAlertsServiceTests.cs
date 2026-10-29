using LimousineBooking.Application.AdminAlerts;
using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using Moq;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainUser = LimousineBooking.Domain.Entities.User;
using UserRole = LimousineBooking.Domain.Enums.UserRole;

namespace LimousineBooking.Tests.AdminAlerts;

public class AdminAlertsServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<ICrmEmailThreadRepository> _threadRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime FixedUtcNow = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public AdminAlertsServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _threadRepository
            .Setup(r => r.SearchAsync(It.IsAny<CrmEmailThreadSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CrmEmailThreadListItemResponse>(), 0));
    }

    private AdminAlertsService CreateService() =>
        new(_bookingRepository.Object, _threadRepository.Object, _userRepository.Object, _dateTimeProvider.Object);

    private static DomainBooking MakeBooking(string reference, DateTime createdAt)
    {
        var booking = new DomainBooking(
            reference, "Jane", "Doe", "jane@example.com", "+41791234567",
            Guid.NewGuid(), DateOnly.FromDateTime(FixedUtcNow), new TimeOnly(10, 0),
            "Airport", 1, 100m, "CHF");
        booking.CreatedAt = createdAt;
        return booking;
    }

    private static DomainUser MakeAdmin(DateTime? bookingsSeenAt)
    {
        var user = new DomainUser("admin@example.com", "hash", "Admin", "User", UserRole.Administrator);
        if (bookingsSeenAt is { } seenAt)
            user.MarkBookingsNotificationsSeen(seenAt);
        return user;
    }

    [Fact]
    public async Task GetSummaryAsync_UserNeverCheckedBefore_TreatsAllBookingsAsNew()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(MakeAdmin(null));
        _bookingRepository.Setup(r => r.CountCreatedAfterAsync(DateTime.MinValue, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _bookingRepository
            .Setup(r => r.GetRecentlyCreatedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeBooking("LM-1", FixedUtcNow.AddMinutes(-5)) });

        var result = await CreateService().GetSummaryAsync(userId);

        Assert.Equal(3, result.NewBookingsCount);
        Assert.True(result.RecentBookings.Single().IsNew);
    }

    [Fact]
    public async Task GetSummaryAsync_BookingCreatedBeforeLastSeen_IsNotMarkedNew()
    {
        var userId = Guid.NewGuid();
        var seenAt = FixedUtcNow.AddHours(-1);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(MakeAdmin(seenAt));
        _bookingRepository.Setup(r => r.CountCreatedAfterAsync(seenAt, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _bookingRepository
            .Setup(r => r.GetRecentlyCreatedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeBooking("LM-1", seenAt.AddMinutes(-30)) });

        var result = await CreateService().GetSummaryAsync(userId);

        Assert.Equal(0, result.NewBookingsCount);
        Assert.False(result.RecentBookings.Single().IsNew);
    }

    [Fact]
    public async Task GetSummaryAsync_ReflectsUnreadEmailThreadCountFromRepository()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(MakeAdmin(FixedUtcNow));
        _bookingRepository.Setup(r => r.CountCreatedAfterAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _bookingRepository.Setup(r => r.GetRecentlyCreatedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DomainBooking>());
        _threadRepository
            .Setup(r => r.SearchAsync(It.Is<CrmEmailThreadSearchQuery>(q => q.UnreadOnly == true), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CrmEmailThreadListItemResponse>(), 7));

        var result = await CreateService().GetSummaryAsync(userId);

        Assert.Equal(7, result.UnreadEmailsCount);
    }

    [Fact]
    public async Task MarkBookingsSeenAsync_UpdatesUserAndSaves()
    {
        var userId = Guid.NewGuid();
        var user = MakeAdmin(null);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateService().MarkBookingsSeenAsync(userId);

        Assert.Equal(FixedUtcNow, user.BookingsNotificationsSeenAt);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkBookingsSeenAsync_UnknownUser_DoesNotThrowOrSave()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        await CreateService().MarkBookingsSeenAsync(userId);

        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
