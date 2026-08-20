using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class RideStatusHistoryRepository : IRideStatusHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RideStatusHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RideStatusHistory>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        await _dbContext.RideStatusHistories
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.ChangedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(RideStatusHistory history, CancellationToken cancellationToken = default)
    {
        _dbContext.RideStatusHistories.Add(history);
        return Task.CompletedTask;
    }
}
