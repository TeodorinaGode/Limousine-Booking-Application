using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// Insert-only audit trail of ride-status transitions — mirrors
/// <see cref="AssignmentHistory"/>'s pattern. A dedicated table rather than
/// reusing <see cref="BookingStatusHistory"/>, since that entity is typed to
/// <see cref="BookingStatus"/>, a different (and independently-tracked) concept.
/// </summary>
public class RideStatusHistory : Entity
{
    public Guid BookingId { get; private set; }
    public RideStatus PreviousStatus { get; private set; }
    public RideStatus NewStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Booking? Booking { get; private set; }

    private RideStatusHistory()
    {
    }

    public RideStatusHistory(Guid bookingId, RideStatus previousStatus, RideStatus newStatus, Guid changedByUserId, DateTime changedAt)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (changedByUserId == Guid.Empty)
            throw new ArgumentException("ChangedByUserId is required.", nameof(changedByUserId));

        BookingId = bookingId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
    }
}
