using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>Administrator-only CRM activity log — append-only (calls, emails, meetings, notes). Never updated or deleted once created.</summary>
[ApiController]
[Route("api/admin/crm/activities")]
[Authorize(Roles = "Administrator")]
public class CrmActivitiesController : ControllerBase
{
    private readonly ICrmActivityService _activityService;
    private readonly ICurrentUserService _currentUserService;

    public CrmActivitiesController(ICrmActivityService activityService, ICurrentUserService currentUserService)
    {
        _activityService = activityService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmActivityResponse>>> GetAll([FromQuery] CrmActivitySearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _activityService.SearchAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CrmActivityResponse>> Create([FromBody] CreateCrmActivityRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        var result = await _activityService.CreateAsync(userId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Activity) : MapError(result);
    }

    private ActionResult MapError(CrmActivityOperationResult result) => result.Error switch
    {
        CrmActivityError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmActivityError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
