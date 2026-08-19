using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
