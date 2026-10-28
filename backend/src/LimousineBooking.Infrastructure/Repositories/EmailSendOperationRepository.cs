using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class EmailSendOperationRepository : IEmailSendOperationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailSendOperationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailSendOperation?> GetByClientOperationIdAsync(Guid clientOperationId, CancellationToken cancellationToken = default) =>
        _dbContext.EmailSendOperations.SingleOrDefaultAsync(o => o.ClientOperationId == clientOperationId, cancellationToken);

    public Task<EmailSendOperation?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken cancellationToken = default) =>
        _dbContext.EmailSendOperations.SingleOrDefaultAsync(o => o.ProviderMessageId == providerMessageId, cancellationToken);

    public Task AddAsync(EmailSendOperation operation, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailSendOperations.Add(operation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
