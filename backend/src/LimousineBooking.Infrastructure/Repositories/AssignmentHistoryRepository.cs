using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class AssignmentHistoryRepository : IAssignmentHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AssignmentHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AssignmentHistory>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        await _dbContext.AssignmentHistories
            .Where(a => a.BookingId == bookingId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AssignmentHistory history, CancellationToken cancellationToken = default)
    {
        _dbContext.AssignmentHistories.Add(history);
        return Task.CompletedTask;
    }
}
