using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// Insert-only audit trail of every successful driver/vehicle assignment for a
/// booking — written by both AutomaticAssignmentService and AdminBookingService's
/// manual assignment path, never updated or deleted, so reassigning a booking never
/// loses the record of who/what it was assigned to before.
/// </summary>
public class AssignmentHistory : Entity
{
    public Guid BookingId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public AssignmentType AssignmentType { get; private set; }

    /// <summary>Null for automatic assignment — there is no acting administrator.</summary>
    public Guid? AssignedByUserId { get; private set; }

    public DateTime AssignedAt { get; private set; }
    public string? Reason { get; private set; }

    public Booking? Booking { get; private set; }

    private AssignmentHistory()
    {
    }

    public AssignmentHistory(Guid bookingId, Guid driverId, Guid vehicleId, AssignmentType assignmentType, Guid? assignedByUserId, DateTime assignedAt, string? reason = null)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        BookingId = bookingId;
        DriverId = driverId;
        VehicleId = vehicleId;
        AssignmentType = assignmentType;
        AssignedByUserId = assignedByUserId;
        AssignedAt = assignedAt;
        Reason = reason;
    }
}
