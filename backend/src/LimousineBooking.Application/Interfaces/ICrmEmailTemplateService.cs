using LimousineBooking.Application.Common;
using LimousineBooking.Application.Email;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmEmailTemplateService
{
    Task<PagedResult<CrmEmailTemplateResponse>> SearchAsync(CrmEmailTemplateSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmEmailTemplateResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmEmailTemplateOperationResult> CreateAsync(CreateCrmEmailTemplateRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<CrmEmailTemplateOperationResult> UpdateAsync(Guid id, UpdateCrmEmailTemplateRequest request, CancellationToken cancellationToken = default);

    Task<CrmEmailTemplateOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default);

    /// <summary>Substitutes the template's {{...}} variables against whichever of company/contact/deal are supplied — null for any the caller doesn't have.</summary>
    Task<CrmEmailTemplateRenderResult?> RenderAsync(Guid templateId, Guid? companyId, Guid? contactId, Guid? dealId, CancellationToken cancellationToken = default);
}
