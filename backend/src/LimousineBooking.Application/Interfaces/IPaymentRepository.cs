using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByProviderCheckoutSessionIdAsync(string providerCheckoutSessionId, CancellationToken cancellationToken = default);

    /// <summary>Every attempt for this booking, most recent first — used for the admin payment-history view.</summary>
    Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>The most recent attempt for this booking, or null if none exists yet.</summary>
    Task<Payment?> GetLatestByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>The successful attempt for this booking, if any — a booking should have at most one.</summary>
    Task<Payment?> GetPaidByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
