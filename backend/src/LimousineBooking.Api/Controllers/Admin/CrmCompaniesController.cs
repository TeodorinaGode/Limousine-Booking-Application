using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Administrator-only CRM company management — B2B partners (hotels, travel
/// agencies, corporate clients, etc.), entirely separate from individual
/// booking customers. Archiving never deletes a company: historical deals,
/// activities, and tasks must remain intact and reportable.
/// </summary>
[ApiController]
[Route("api/admin/crm/companies")]
[Authorize(Roles = "Administrator")]
public class CrmCompaniesController : ControllerBase
{
    private readonly ICrmCompanyService _companyService;

    public CrmCompaniesController(ICrmCompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmCompanyResponse>>> GetAll([FromQuery] CrmCompanySearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _companyService.SearchAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CrmCompanyDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _companyService.GetDetailByIdAsync(id, cancellationToken);
        return detail is null ? NotFound(new { message = "Company not found." }) : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<CrmCompanyResponse>> Create([FromBody] CreateCrmCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.CreateAsync(request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Company!.Id }, result.Company)
            : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CrmCompanyResponse>> Update(Guid id, [FromBody] UpdateCrmCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<CrmCompanyResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _companyService.SetArchivedAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpPut("{id:guid}/unarchive")]
    public async Task<ActionResult<CrmCompanyResponse>> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _companyService.SetArchivedAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpPut("{id:guid}/contacted")]
    public async Task<ActionResult<CrmCompanyResponse>> RecordContacted(Guid id, [FromBody] RecordContactedRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.RecordContactedAsync(id, request.ContactedAt, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpPut("{id:guid}/follow-up")]
    public async Task<ActionResult<CrmCompanyResponse>> ScheduleFollowUp(Guid id, [FromBody] ScheduleFollowUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.ScheduleFollowUpAsync(id, request.FollowUpAt, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<CrmCompanyResponse>> LinkContact(Guid id, [FromBody] LinkCrmContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.LinkContactAsync(id, request.ContactId, request.IsPrimary, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public async Task<ActionResult<CrmCompanyResponse>> UnlinkContact(Guid id, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _companyService.UnlinkContactAsync(id, contactId, cancellationToken);
        return result.Succeeded ? Ok(result.Company) : MapError(result);
    }

    private ActionResult MapError(CrmCompanyOperationResult result) => result.Error switch
    {
        CrmCompanyError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmCompanyError.Duplicate => Conflict(new { message = result.ErrorMessage }),
        CrmCompanyError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
