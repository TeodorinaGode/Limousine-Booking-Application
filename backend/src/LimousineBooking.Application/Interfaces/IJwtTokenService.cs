using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

public record JwtToken(string AccessToken, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed access token for the given user. <paramref name="driver"/> is
    /// supplied when the user is a Driver so a driverId claim can be included.
    /// </summary>
    JwtToken GenerateToken(User user, Driver? driver);
}
