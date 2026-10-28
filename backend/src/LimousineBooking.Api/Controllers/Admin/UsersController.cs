using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>Administrator-only user directory lookups — currently just the administrator list used to populate CRM owner-assignment dropdowns.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private readonly IAdministratorDirectoryService _administratorDirectoryService;

    public UsersController(IAdministratorDirectoryService administratorDirectoryService)
    {
        _administratorDirectoryService = administratorDirectoryService;
    }

    [HttpGet("administrators")]
    public async Task<ActionResult<IReadOnlyList<AdministratorSummaryResponse>>> GetAdministrators(CancellationToken cancellationToken) =>
        Ok(await _administratorDirectoryService.GetAdministratorsAsync(cancellationToken));
}
