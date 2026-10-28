using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmEmailMessageRepository : ICrmEmailMessageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmEmailMessageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid connectionId, string providerMessageId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailMessages.AnyAsync(m => m.ConnectionId == connectionId && m.ProviderMessageId == providerMessageId, cancellationToken);

    public Task<CrmEmailMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailMessages
            .Include(m => m.Attachments)
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<CrmEmailMessage?> GetByConnectionAndProviderMessageIdAsync(Guid connectionId, string providerMessageId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailMessages
            .Include(m => m.Attachments)
            .SingleOrDefaultAsync(m => m.ConnectionId == connectionId && m.ProviderMessageId == providerMessageId, cancellationToken);

    public Task<CrmEmailMessage?> GetBySendOperationIdAsync(Guid sendOperationId, CancellationToken cancellationToken = default) =>
        _dbContext.CrmEmailMessages
            .Include(m => m.Attachments)
            .SingleOrDefaultAsync(m => m.SendOperationId == sendOperationId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetThreadIdsByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
            return new Dictionary<Guid, Guid>();

        return await _dbContext.CrmEmailMessages
            .Where(m => messageIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.ThreadId, cancellationToken);
    }

    public Task AddAsync(CrmEmailMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task AddAttachmentAsync(CrmEmailAttachment attachment, CancellationToken cancellationToken = default)
    {
        _dbContext.CrmEmailAttachments.Add(attachment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
