using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class ContactMessageRepository : IContactMessageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ContactMessageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ContactMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.ContactMessages.Add(message);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ContactMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        await _dbContext.ContactMessages
            .Where(c => c.Status == ContactMessageStatus.Pending)
            .OrderBy(c => c.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
