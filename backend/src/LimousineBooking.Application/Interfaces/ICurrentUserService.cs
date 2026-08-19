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
}
