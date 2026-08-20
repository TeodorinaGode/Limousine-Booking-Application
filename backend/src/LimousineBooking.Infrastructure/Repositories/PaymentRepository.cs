using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetByProviderCheckoutSessionIdAsync(string providerCheckoutSessionId, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.SingleOrDefaultAsync(p => p.ProviderCheckoutSessionId == providerCheckoutSessionId, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        await _dbContext.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Payment?> GetLatestByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _dbContext.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Payment?> GetPaidByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _dbContext.Payments.SingleOrDefaultAsync(p => p.BookingId == bookingId && p.Status == PaymentStatus.Paid, cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
