using LimousineBooking.Application.Account;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers;

/// <summary>
/// Self-service preferences for the currently authenticated user (Administrator
/// or Driver) — currently just the language preference (Prompt 16, section 21).
/// Available to any authenticated role, unlike the role-scoped Admin/Driver
/// controllers, since both roles use it identically.
/// </summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUserService;

    public AccountController(IAccountService accountService, ICurrentUserService currentUserService)
    {
        _accountService = accountService;
        _currentUserService = currentUserService;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<AccountPreferencesResponse>> GetPreferences(CancellationToken cancellationToken)
    {
        var preferences = await _accountService.GetPreferencesAsync(_currentUserService.UserId!.Value, cancellationToken);
        return preferences is null ? NotFound(new { message = "User not found." }) : Ok(preferences);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<AccountPreferencesResponse>> UpdatePreferences([FromBody] UpdateAccountPreferencesRequest request, CancellationToken cancellationToken)
    {
        var preferences = await _accountService.UpdatePreferencesAsync(_currentUserService.UserId!.Value, request, cancellationToken);
        return preferences is null ? NotFound(new { message = "User not found." }) : Ok(preferences);
    }
}
