using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Email;

public class CrmEmailTemplateService : ICrmEmailTemplateService
{
    private readonly ICrmEmailTemplateRepository _templateRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmDealRepository _dealRepository;

    public CrmEmailTemplateService(
        ICrmEmailTemplateRepository templateRepository,
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        ICrmDealRepository dealRepository)
    {
        _templateRepository = templateRepository;
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _dealRepository = dealRepository;
    }

    public async Task<PagedResult<CrmEmailTemplateResponse>> SearchAsync(CrmEmailTemplateSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _templateRepository.SearchAsync(query, cancellationToken);
        return new PagedResult<CrmEmailTemplateResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmEmailTemplateResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        return template is null ? null : ToResponse(template);
    }

    public async Task<CrmEmailTemplateOperationResult> CreateAsync(CreateCrmEmailTemplateRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = new CrmEmailTemplate(request.Name, request.Subject, request.BodyHtml, request.Category, request.Language, userId);
            await _templateRepository.AddAsync(template, cancellationToken);
            await _templateRepository.SaveChangesAsync(cancellationToken);
            return CrmEmailTemplateOperationResult.Success(ToResponse(template));
        }
        catch (ArgumentException ex)
        {
            return CrmEmailTemplateOperationResult.Failure(CrmEmailTemplateError.Validation, ex.Message);
        }
    }

    public async Task<CrmEmailTemplateOperationResult> UpdateAsync(Guid id, UpdateCrmEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (template is null)
            return CrmEmailTemplateOperationResult.Failure(CrmEmailTemplateError.NotFound, "Template not found.");

        try
        {
            template.UpdateDetails(request.Name, request.Subject, request.BodyHtml, request.Category, request.Language);
        }
        catch (ArgumentException ex)
        {
            return CrmEmailTemplateOperationResult.Failure(CrmEmailTemplateError.Validation, ex.Message);
        }

        await _templateRepository.SaveChangesAsync(cancellationToken);
        return CrmEmailTemplateOperationResult.Success(ToResponse(template));
    }

    public async Task<CrmEmailTemplateOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (template is null)
            return CrmEmailTemplateOperationResult.Failure(CrmEmailTemplateError.NotFound, "Template not found.");

        if (isArchived)
            template.Archive();
        else
            template.Unarchive();

        await _templateRepository.SaveChangesAsync(cancellationToken);
        return CrmEmailTemplateOperationResult.Success(ToResponse(template));
    }

    public async Task<CrmEmailTemplateRenderResult?> RenderAsync(Guid templateId, Guid? companyId, Guid? contactId, Guid? dealId, CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return null;

        var company = companyId.HasValue ? await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken) : null;
        var contact = contactId.HasValue ? await _contactRepository.GetByIdAsync(contactId.Value, cancellationToken) : null;
        var deal = dealId.HasValue ? await _dealRepository.GetByIdAsync(dealId.Value, cancellationToken) : null;
        var owner = deal?.Owner ?? company?.AssignedOwner ?? contact?.AssignedOwner;

        var values = new Dictionary<string, string>();
        if (contact is not null)
        {
            values["contact.firstName"] = contact.FirstName;
            values["contact.lastName"] = contact.LastName;
            values["contact.fullName"] = $"{contact.FirstName} {contact.LastName}";
            values["contact.email"] = contact.Email;
        }
        if (company is not null)
        {
            values["company.name"] = company.Name;
        }
        if (deal is not null)
        {
            values["deal.name"] = deal.Name;
            values["deal.value"] = deal.Value.ToString("0.##");
            values["deal.currency"] = deal.Currency;
        }
        if (owner is not null)
        {
            values["owner.firstName"] = owner.FirstName;
            values["owner.lastName"] = owner.LastName;
            values["owner.email"] = owner.Email;
        }

        var (subject, subjectUnresolved) = TemplateVariableRenderer.Render(template.Subject, values);
        var (bodyHtml, bodyUnresolved) = TemplateVariableRenderer.Render(template.BodyHtml, values);

        return new CrmEmailTemplateRenderResult
        {
            Subject = subject,
            BodyHtml = bodyHtml,
            UnresolvedVariables = subjectUnresolved.Concat(bodyUnresolved).Distinct().ToList()
        };
    }

    private static CrmEmailTemplateResponse ToResponse(CrmEmailTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Subject = template.Subject,
        BodyHtml = template.BodyHtml,
        Category = template.Category,
        Language = template.Language,
        IsArchived = template.IsArchived,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt
    };
}
