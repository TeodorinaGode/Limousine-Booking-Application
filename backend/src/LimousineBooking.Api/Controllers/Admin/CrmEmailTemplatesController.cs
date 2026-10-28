using LimousineBooking.Application.Common;
using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>Reusable, personalizable email drafts inserted into the composer — never sent directly. See CrmEmailTemplateService.RenderAsync.</summary>
[ApiController]
[Route("api/admin/crm/email/templates")]
[Authorize(Roles = "Administrator")]
public class CrmEmailTemplatesController : ControllerBase
{
    private readonly ICrmEmailTemplateService _templateService;
    private readonly ICurrentUserService _currentUserService;

    public CrmEmailTemplatesController(ICrmEmailTemplateService templateService, ICurrentUserService currentUserService)
    {
        _templateService = templateService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CrmEmailTemplateResponse>>> GetAll([FromQuery] CrmEmailTemplateSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await _templateService.SearchAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CrmEmailTemplateResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdAsync(id, cancellationToken);
        return template is null ? NotFound(new { message = "Template not found." }) : Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<CrmEmailTemplateResponse>> Create([FromBody] CreateCrmEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        var result = await _templateService.CreateAsync(request, userId, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Template!.Id }, result.Template)
            : MapError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CrmEmailTemplateResponse>> Update(Guid id, [FromBody] UpdateCrmEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _templateService.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Template) : MapError(result);
    }

    [HttpPut("{id:guid}/archive")]
    public async Task<ActionResult<CrmEmailTemplateResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _templateService.SetArchivedAsync(id, true, cancellationToken);
        return result.Succeeded ? Ok(result.Template) : MapError(result);
    }

    [HttpPut("{id:guid}/unarchive")]
    public async Task<ActionResult<CrmEmailTemplateResponse>> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _templateService.SetArchivedAsync(id, false, cancellationToken);
        return result.Succeeded ? Ok(result.Template) : MapError(result);
    }

    [HttpGet("{id:guid}/render")]
    public async Task<ActionResult<CrmEmailTemplateRenderResult>> Render(Guid id, [FromQuery] Guid? companyId, [FromQuery] Guid? contactId, [FromQuery] Guid? dealId, CancellationToken cancellationToken)
    {
        var result = await _templateService.RenderAsync(id, companyId, contactId, dealId, cancellationToken);
        return result is null ? NotFound(new { message = "Template not found." }) : Ok(result);
    }

    private ActionResult MapError(CrmEmailTemplateOperationResult result) => result.Error switch
    {
        CrmEmailTemplateError.NotFound => NotFound(new { message = result.ErrorMessage }),
        CrmEmailTemplateError.Validation => BadRequest(new { message = result.ErrorMessage }),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}
