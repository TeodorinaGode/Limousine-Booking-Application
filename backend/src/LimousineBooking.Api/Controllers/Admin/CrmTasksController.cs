using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>Administrator-only CRM follow-up task management.</summary>
[ApiController]
[Route("api/admin/crm/tasks")]
[Authorize(Roles = "Administrator")]
public class CrmTasksController : ControllerBase
{
    private readonly ICrmTaskService _taskService;

    public CrmTasksController(ICrmTaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmTaskResponse>>> GetAll([FromQuery] CrmTaskSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _taskService.SearchAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CrmTaskResponse>> Create([FromBody] CreateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Task) : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CrmTaskResponse>> Update(Guid id, [FromBody] UpdateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Task) : MapError(result);
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<ActionResult<CrmTaskResponse>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.CompleteAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Task) : MapError(result);
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<CrmTaskResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.CancelAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Task) : MapError(result);
    }

    private ActionResult MapError(CrmTaskOperationResult result) => result.Error switch
    {
        CrmTaskError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmTaskError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
