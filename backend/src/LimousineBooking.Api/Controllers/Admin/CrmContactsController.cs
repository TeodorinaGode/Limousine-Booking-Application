using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>Administrator-only CRM contact management — people at partner companies, distinct from individual booking customers.</summary>
[ApiController]
[Route("api/admin/crm/contacts")]
[Authorize(Roles = "Administrator")]
public class CrmContactsController : ControllerBase
{
    private readonly ICrmContactService _contactService;

    public CrmContactsController(ICrmContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmContactResponse>>> GetAll([FromQuery] CrmContactSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _contactService.SearchAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CrmContactDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _contactService.GetDetailByIdAsync(id, cancellationToken);
        return detail is null ? NotFound(new { message = "Contact not found." }) : Ok(detail);
    }

    /// <response code="409">A contact with this email already exists.</response>
    [HttpPost]
    public async Task<ActionResult<CrmContactResponse>> Create([FromBody] CreateCrmContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _contactService.CreateAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Contact!.Id }, result.Contact)
            : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CrmContactResponse>> Update(Guid id, [FromBody] UpdateCrmContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _contactService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Contact) : MapError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<CrmContactResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _contactService.SetArchivedAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Contact) : MapError(result);
    }

    [HttpPut("{id:guid}/unarchive")]
    public async Task<ActionResult<CrmContactResponse>> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _contactService.SetArchivedAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Contact) : MapError(result);
    }

    [HttpPut("{id:guid}/contacted")]
    public async Task<ActionResult<CrmContactResponse>> RecordContacted(Guid id, [FromBody] RecordContactedRequest request, CancellationToken cancellationToken)
    {
        var result = await _contactService.RecordContactedAsync(id, request.ContactedAt, cancellationToken);
        return result.Succeeded ? Ok(result.Contact) : MapError(result);
    }

    [HttpPut("{id:guid}/follow-up")]
    public async Task<ActionResult<CrmContactResponse>> ScheduleFollowUp(Guid id, [FromBody] ScheduleFollowUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _contactService.ScheduleFollowUpAsync(id, request.FollowUpAt, cancellationToken);
        return result.Succeeded ? Ok(result.Contact) : MapError(result);
    }

    private ActionResult MapError(CrmContactOperationResult result) => result.Error switch
    {
        CrmContactError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmContactError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        CrmContactError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
