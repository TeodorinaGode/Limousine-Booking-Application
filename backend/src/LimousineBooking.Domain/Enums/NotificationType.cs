namespace LimousineBooking.Domain.Enums;

public enum NotificationType
{
    /// <summary>Customer — automatic assignment succeeded at booking creation (or a later revalidation).</summary>
    BookingConfirmation,

    /// <summary>Customer — automatic assignment could not find a driver; booking stays Pending.</summary>
    BookingPending,

    /// <summary>Customer — an administrator manually assigned a driver for the first time (not a reassignment).</summary>
    CustomerAssigned,

    /// <summary>Customer — the booking's driver was changed (reassignment). Trip details are unchanged.</summary>
    BookingReassigned,

    BookingCancellation,

    RideCompleted,

    /// <summary>Driver — newly assigned to a booking (first assignment, admin assignment, or the new driver in a reassignment).</summary>
    DriverAssignment,

    /// <summary>Driver — was previously assigned but the booking was reassigned to someone else.</summary>
    DriverReassignedAway,

    /// <summary>Admin/operations — automatic assignment failed and the booking needs a manual driver.</summary>
    ManualAssignmentRequired
}
