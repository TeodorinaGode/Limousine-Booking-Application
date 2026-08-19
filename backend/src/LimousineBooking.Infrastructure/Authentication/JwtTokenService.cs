using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LimousineBooking.Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public JwtToken GenerateToken(User user, Driver? driver)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email", user.Email),
            new("role", user.Role.ToString()),
            new("name", $"{user.FirstName} {user.LastName}")
        };

        if (driver is not null)
            claims.Add(new Claim("driverId", driver.Id.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtToken(accessToken, expiresAtUtc);
    }
}
