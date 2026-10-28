using System.Security.Cryptography;
using System.Text;
using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.Authentication;

/// <summary>
/// HMAC-SHA256-signed, time-limited state token: "{userId}.{nonce}.{unixSeconds}.{signatureBase64Url}".
/// Reuses JwtSettings.SecretKey — an existing, already-securely-managed signing
/// key — rather than provisioning a whole new secret just for this incidental
/// signing need. The userId is carried here (not read from the request)
/// because the OAuth callback is a plain browser redirect from the provider
/// with no bearer token attached.
/// </summary>
public class OAuthStateService : IOAuthStateService
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);

    private readonly JwtSettings _jwtSettings;

    public OAuthStateService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string CreateState(Guid userId)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{userId}.{nonce}.{timestamp}";
        var signature = Sign(payload);
        return $"{payload}.{signature}";
    }

    public bool TryValidateState(string state, out Guid userId)
    {
        userId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        var parts = state.Split('.');
        if (parts.Length != 4)
            return false;

        var payload = $"{parts[0]}.{parts[1]}.{parts[2]}";
        var expectedSignature = Sign(payload);

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(parts[3])))
            return false;

        if (!Guid.TryParse(parts[0], out var parsedUserId))
            return false;

        if (!long.TryParse(parts[2], out var timestamp))
            return false;

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (DateTimeOffset.UtcNow - issuedAt > MaxAge || issuedAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return false;

        userId = parsedUserId;
        return true;
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
