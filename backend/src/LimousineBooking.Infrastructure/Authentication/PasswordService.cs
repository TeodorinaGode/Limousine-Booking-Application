using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LimousineBooking.Infrastructure.Authentication;

/// <summary>
/// Wraps ASP.NET Core Identity's <see cref="IPasswordHasher{TUser}"/> (PBKDF2-based,
/// no custom cryptography) behind the Application layer's <see cref="IPasswordService"/>.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string Hash(string password) => _passwordHasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(null!, passwordHash, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
