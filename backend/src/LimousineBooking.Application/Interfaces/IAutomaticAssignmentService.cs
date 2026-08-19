namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Attempts to find an eligible driver + vehicle for a freshly created booking
/// and assign them automatically. Never throws to signal "no driver available" —
/// that outcome is recorded on the booking itself (RequiresManualAssignment),
/// not surfaced as a failure to the caller.
/// </summary>
public interface IAutomaticAssignmentService
{
    Task AssignBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
