using LimousineBooking.Application.Authentication;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _authService.LoginAsync(request, cancellationToken);

        if (!outcome.Succeeded)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(outcome.Response);
    }
}
