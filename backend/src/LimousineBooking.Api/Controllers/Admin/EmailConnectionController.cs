using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Manages the CRM's one shared Gmail connection. The callback action is the
/// one exception to this controller's [Authorize] — it's hit by a plain
/// browser redirect from Google with no bearer token, so its only security
/// is the signed OAuth state parameter (see OAuthStateService), not the JWT
/// middleware. Every other action requires Administrator exactly like every
/// other admin controller in this app.
/// </summary>
[ApiController]
[Route("api/admin/crm/email/connection")]
[Authorize(Roles = "Administrator")]
public class EmailConnectionController : ControllerBase
{
    private readonly IEmailConnectionService _connectionService;
    private readonly ICurrentUserService _currentUserService;

    public EmailConnectionController(IEmailConnectionService connectionService, ICurrentUserService currentUserService)
    {
        _connectionService = connectionService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<EmailConnectionStatusResponse>> GetStatus(CancellationToken cancellationToken) =>
        Ok(await _connectionService.GetStatusAsync(cancellationToken));

    /// <summary>Also used for "Reconnect" — starting a fresh OAuth flow is the same operation either way.</summary>
    [HttpGet("authorize")]
    public async Task<ActionResult<AuthorizationUrlResponse>> Authorize(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        try
        {
            return Ok(await _connectionService.BuildAuthorizationUrlAsync(userId, cancellationToken));
        }
        catch (EmailProviderNotConfiguredException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>Google redirects the browser here directly — never called by the frontend's own fetch client.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery(Name = "error")] string? providerError, CancellationToken cancellationToken)
    {
        var redirectUrl = await _connectionService.HandleOAuthCallbackAsync(code, state, providerError, cancellationToken);
        return Redirect(redirectUrl);
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await _connectionService.DisconnectAsync(cancellationToken);
        return NoContent();
    }
}
