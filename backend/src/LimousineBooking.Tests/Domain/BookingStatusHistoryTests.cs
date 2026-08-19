using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class BookingStatusHistoryTests
{
    [Fact]
    public void BookingStatusHistory_CanBeCreated()
    {
        var bookingId = Guid.NewGuid();

        var history = new BookingStatusHistory(
            bookingId,
            previousStatus: BookingStatus.Pending,
            newStatus: BookingStatus.Confirmed,
            changedByUserId: Guid.NewGuid(),
            notes: "Confirmed by admin");

        Assert.Equal(bookingId, history.BookingId);
        Assert.Equal(BookingStatus.Pending, history.PreviousStatus);
        Assert.Equal(BookingStatus.Confirmed, history.NewStatus);
    }

    [Fact]
    public void BookingStatusHistory_AllowsNullPreviousStatus_ForInitialEntry()
    {
        var history = new BookingStatusHistory(Guid.NewGuid(), previousStatus: null, newStatus: BookingStatus.Pending);

        Assert.Null(history.PreviousStatus);
    }

    [Fact]
    public void BookingStatusHistory_AllowsNullChangedByUserId_ForSystemChanges()
    {
        var history = new BookingStatusHistory(Guid.NewGuid(), BookingStatus.Assigned, BookingStatus.OnTheWay, changedByUserId: null);

        Assert.Null(history.ChangedByUserId);
    }
}
