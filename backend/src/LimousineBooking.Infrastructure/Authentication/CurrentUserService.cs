using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LimousineBooking.Infrastructure.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirst("sub")?.Value, out var id) ? id : null;

    public string? Email => User?.FindFirst("email")?.Value;

    public string? Role => User?.FindFirst("role")?.Value;
}
