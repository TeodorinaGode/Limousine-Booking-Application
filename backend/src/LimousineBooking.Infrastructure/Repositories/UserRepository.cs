using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<bool> HasDuplicateEmailAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.Where(u => EF.Functions.ILike(u.Email, email));

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAdministratorsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Users
            .Where(u => u.Role == UserRole.Administrator && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
