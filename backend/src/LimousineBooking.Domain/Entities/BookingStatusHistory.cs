using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

public class BookingStatusHistory : Entity
{
    public Guid BookingId { get; private set; }
    public BookingStatus? PreviousStatus { get; private set; }
    public BookingStatus NewStatus { get; private set; }
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;
    public Guid? ChangedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public Booking? Booking { get; private set; }
    public User? ChangedByUser { get; private set; }

    private BookingStatusHistory()
    {
    }

    public BookingStatusHistory(Guid bookingId, BookingStatus? previousStatus, BookingStatus newStatus, Guid? changedByUserId = null, string? notes = null)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));

        BookingId = bookingId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        Notes = notes;
    }
}
