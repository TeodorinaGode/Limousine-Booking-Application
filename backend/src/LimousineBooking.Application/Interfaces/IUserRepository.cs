using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>True if any user (case-insensitive) already has this email, excluding <paramref name="excludeUserId"/> if given.</summary>
    Task<bool> HasDuplicateEmailAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
