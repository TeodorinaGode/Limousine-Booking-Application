using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers;

/// <summary>
/// Development-only endpoints for verifying JWT authentication and role-based
/// authorization end-to-end (including from Swagger). Not part of the product
/// API surface — safe to remove once real protected endpoints exist.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult Authenticated() => Ok(new { message = "You are authenticated." });

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator")]
    public IActionResult AdminOnly() => Ok(new { message = "You are an administrator." });

    [HttpGet("driver")]
    [Authorize(Roles = "Driver")]
    public IActionResult DriverOnly() => Ok(new { message = "You are a driver." });
}
