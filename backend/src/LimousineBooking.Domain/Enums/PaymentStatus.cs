namespace LimousineBooking.Domain.Enums;

/// <summary>
/// The payment lifecycle — deliberately independent of <see cref="BookingStatus"/>.
/// A booking's confirmation already depends only on driver/vehicle assignment
/// (see AutomaticAssignmentService); payment is a parallel concern layered on
/// top, never folded into BookingStatus.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Paid,
    Failed,
    Cancelled,
    Refunded
}
