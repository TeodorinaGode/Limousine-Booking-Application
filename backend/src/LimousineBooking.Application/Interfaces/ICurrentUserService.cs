namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Abstraction over the authenticated HTTP request's identity, so application
/// code does not need to depend on HttpContext directly.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }

    /// <summary>Present only for Driver-role tokens (see JwtTokenService). Never trust a driver id from a request body/URL instead.</summary>
    Guid? DriverId { get; }
}
