using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class EmailSyncCheckpointRepository : IEmailSyncCheckpointRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailSyncCheckpointRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailSyncCheckpoint?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default) =>
        _dbContext.EmailSyncCheckpoints.SingleOrDefaultAsync(c => c.ConnectionId == connectionId, cancellationToken);

    public Task AddAsync(EmailSyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailSyncCheckpoints.Add(checkpoint);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
