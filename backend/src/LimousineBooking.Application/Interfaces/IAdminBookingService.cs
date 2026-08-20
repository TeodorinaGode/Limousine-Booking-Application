using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Common;

namespace LimousineBooking.Application.Interfaces;

public interface IAdminBookingService
{
    Task<PagedResult<AdminBookingListItemResponse>> SearchAsync(AdminBookingSearchQuery query, CancellationToken cancellationToken = default);

    Task<AdminBookingDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminBookingOperationResult> UpdateAsync(Guid id, UpdateBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>Handles both first-time manual assignment and reassignment — see AdminBookingService.</summary>
    Task<AdminBookingOperationResult> AssignDriverAsync(Guid id, AssignDriverRequest request, CancellationToken cancellationToken = default);

    Task<AdminBookingOperationResult> CancelAsync(Guid id, CancelBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>Re-invokes AutomaticAssignmentService for a booking that currently requires manual assignment (or whose assignment an administrator wants recomputed).</summary>
    Task<AdminBookingOperationResult> AutoAssignAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-enqueues the confirmation email using the booking's current state. Read-only — never touches status/assignment, never creates a duplicate booking.</summary>
    Task<AdminBookingOperationResult> ResendConfirmationAsync(Guid id, CancellationToken cancellationToken = default);
}
