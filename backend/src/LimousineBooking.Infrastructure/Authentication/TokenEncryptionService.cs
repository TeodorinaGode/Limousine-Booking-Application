using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LimousineBooking.Infrastructure.Authentication;

/// <summary>
/// Encrypts OAuth refresh tokens at rest using ASP.NET Core's built-in Data
/// Protection API — ships with the framework, no new package, no hand-rolled
/// cryptography. The protector's purpose string scopes the key derivation to
/// this exact use, so it can never decrypt ciphertext from an unrelated
/// Data-Protection consumer even if one is added later.
/// </summary>
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("LimousineBooking.CrmEmail.RefreshToken.v1");
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
}
