using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class EmailProviderConnectionRepository : IEmailProviderConnectionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailProviderConnectionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailProviderConnection?> GetAsync(CancellationToken cancellationToken = default) =>
        _dbContext.EmailProviderConnections
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<EmailProviderConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.EmailProviderConnections.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task AddAsync(EmailProviderConnection connection, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailProviderConnections.Add(connection);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
