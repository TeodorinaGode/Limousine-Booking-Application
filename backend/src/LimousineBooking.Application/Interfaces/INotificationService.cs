using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// The single place booking/assignment services call to trigger a notification.
/// Implementations only ever enqueue an OutboxMessage (via IOutboxRepository) —
/// they never send an email directly, so calling any of these methods can never
/// itself fail the caller's business transaction on a provider outage. Callers
/// still need to call their own SaveChangesAsync afterward; the enqueue rides
/// along in the same DbContext unit of work.
/// </summary>
public interface INotificationService
{
    Task NotifyCustomerBookingConfirmedAsync(Booking booking, Route route, CancellationToken cancellationToken = default);

    Task NotifyCustomerBookingPendingAsync(Booking booking, Route route, CancellationToken cancellationToken = default);

    Task NotifyDriverAssignedAsync(Booking booking, Route route, Driver driver, CancellationToken cancellationToken = default);

    /// <summary>Customer-facing "a driver has been assigned" — used for an administrator's first-time manual assignment (automatic assignment uses the Confirmed notification instead).</summary>
    Task NotifyCustomerAssignedAsync(Booking booking, Route route, Driver driver, CancellationToken cancellationToken = default);

    /// <summary>Fires all three reassignment notifications: previous driver, new driver, customer.</summary>
    Task NotifyReassignedAsync(Booking booking, Route route, Driver previousDriver, Driver newDriver, CancellationToken cancellationToken = default);

    Task NotifyCustomerCancelledAsync(Booking booking, Route route, CancellationToken cancellationToken = default);

    /// <summary>Prepared for the future driver ride-completion feature — no caller exists yet.</summary>
    Task NotifyCustomerCompletedAsync(Booking booking, Route route, CancellationToken cancellationToken = default);

    Task NotifyAdminManualAssignmentRequiredAsync(Booking booking, Route route, string reason, CancellationToken cancellationToken = default);

    /// <summary>Re-enqueues the confirmation email using the booking's current state — read-only, never touches booking status/assignment.</summary>
    Task ResendConfirmationAsync(Booking booking, Route route, CancellationToken cancellationToken = default);

    /// <summary>Customer — an online payment for the booking was confirmed by the payment webhook. Includes the paid amount, never Stripe/internal payment details.</summary>
    Task NotifyPaymentSucceededAsync(Booking booking, Route route, Payment payment, CancellationToken cancellationToken = default);
}
