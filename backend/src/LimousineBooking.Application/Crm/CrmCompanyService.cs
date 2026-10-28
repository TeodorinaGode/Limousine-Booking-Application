using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainCrmCompany = LimousineBooking.Domain.Entities.CrmCompany;
using DomainCrmCompanyContact = LimousineBooking.Domain.Entities.CrmCompanyContact;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Application.Crm;

public class CrmCompanyService : ICrmCompanyService
{
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmEmailMessageRepository _emailMessageRepository;

    public CrmCompanyService(
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        IUserRepository userRepository,
        ICrmActivityRepository activityRepository,
        ICrmEmailMessageRepository emailMessageRepository)
    {
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _userRepository = userRepository;
        _activityRepository = activityRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<PagedResult<CrmCompanyResponse>> SearchAsync(CrmCompanySearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _companyRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<CrmCompanyResponse>
        {
            Items = items.Select(c => ToResponse(c, c.AssignedOwner)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmCompanyDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetDetailByIdAsync(id, cancellationToken);
        if (company is null)
            return null;

        var recentActivities = await _activityRepository.GetRecentForAsync(id, null, null, 10, cancellationToken);

        return new CrmCompanyDetailResponse
        {
            RecentActivities = await CrmActivityResponseMapper.ToResponsesAsync(recentActivities, _emailMessageRepository, cancellationToken),
            Company = ToResponse(company, company.AssignedOwner),
            Contacts = company.CompanyContacts
                .Select(cc => new CrmContactSummary
                {
                    Id = cc.Contact!.Id,
                    FirstName = cc.Contact.FirstName,
                    LastName = cc.Contact.LastName,
                    Email = cc.Contact.Email,
                    JobTitle = cc.Contact.JobTitle,
                    IsPrimary = cc.IsPrimary
                })
                .ToList(),
            Deals = company.Deals
                .Select(d => new CrmDealSummary
                {
                    Id = d.Id,
                    Name = d.Name,
                    Value = d.Value,
                    Currency = d.Currency,
                    StageId = d.StageId,
                    StageName = d.Stage?.Name ?? string.Empty,
                    Probability = d.Probability,
                    IsWon = d.Stage?.IsWon ?? false,
                    IsLost = d.Stage?.IsLost ?? false
                })
                .ToList(),
            OpenTasks = company.Tasks
                .Select(t => new CrmTaskSummary
                {
                    Id = t.Id,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString()
                })
                .ToList()
        };
    }

    public async Task<CrmCompanyOperationResult> CreateAsync(CreateCrmCompanyRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CrmCompanyType>(request.CompanyType, true, out var companyType))
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, "A valid company type is required.");

        var ownerCheck = await ValidateOwnerAsync(request.AssignedOwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        DomainCrmCompany company;
        try
        {
            company = new DomainCrmCompany(request.Name, companyType);
            ApplyDetails(company, request.Name, companyType, request.Status, request.Priority, request.Website, request.Domain,
                request.Industry, request.Country, request.City, request.Address, request.Phone, request.Email,
                request.AssignedOwnerId, request.LeadSource, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, ex.Message);
        }

        await _companyRepository.AddAsync(company, cancellationToken);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, ownerCheck.Owner));
    }

    public async Task<CrmCompanyOperationResult> UpdateAsync(Guid id, UpdateCrmCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        if (!Enum.TryParse<CrmCompanyType>(request.CompanyType, true, out var companyType))
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, "A valid company type is required.");

        var ownerCheck = await ValidateOwnerAsync(request.AssignedOwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        try
        {
            ApplyDetails(company, request.Name, companyType, request.Status, request.Priority, request.Website, request.Domain,
                request.Industry, request.Country, request.City, request.Address, request.Phone, request.Email,
                request.AssignedOwnerId, request.LeadSource, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, ex.Message);
        }

        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, ownerCheck.Owner));
    }

    public async Task<CrmCompanyOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        if (isArchived) company.Archive(); else company.Unarchive();

        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, company.AssignedOwner));
    }

    public async Task<CrmCompanyOperationResult> RecordContactedAsync(Guid id, DateTime contactedAtUtc, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        company.RecordContact(contactedAtUtc);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, company.AssignedOwner));
    }

    public async Task<CrmCompanyOperationResult> ScheduleFollowUpAsync(Guid id, DateTime? followUpAtUtc, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        company.ScheduleFollowUp(followUpAtUtc);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, company.AssignedOwner));
    }

    public async Task<CrmCompanyOperationResult> LinkContactAsync(Guid companyId, Guid contactId, bool isPrimary, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        var contact = await _contactRepository.GetByIdAsync(contactId, cancellationToken);
        if (contact is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, "Contact not found.");

        var existing = await _companyRepository.GetCompanyContactLinkAsync(companyId, contactId, cancellationToken);
        if (existing is not null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Duplicate, "This contact is already linked to this company.");

        await _companyRepository.AddCompanyContactAsync(new DomainCrmCompanyContact(companyId, contactId, isPrimary), cancellationToken);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, company.AssignedOwner));
    }

    public async Task<CrmCompanyOperationResult> UnlinkContactAsync(Guid companyId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.NotFound, "Company not found.");

        var link = await _companyRepository.GetCompanyContactLinkAsync(companyId, contactId, cancellationToken);
        if (link is null)
            return CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, "This contact is not linked to this company.");

        await _companyRepository.RemoveCompanyContactAsync(link, cancellationToken);
        await _companyRepository.SaveChangesAsync(cancellationToken);

        return CrmCompanyOperationResult.Success(ToResponse(company, company.AssignedOwner));
    }

    /// <summary>
    /// Resolves the owner explicitly (rather than trusting the entity's
    /// AssignedOwner navigation, which Create never has loaded and Update
    /// may have loaded stale if the id just changed) — same reasoning as
    /// DriverService's explicit-vehicle validation.
    /// </summary>
    private async Task<(DomainUser? Owner, CrmCompanyOperationResult? Error)> ValidateOwnerAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (!ownerId.HasValue)
            return (null, null);

        var owner = await _userRepository.GetByIdAsync(ownerId.Value, cancellationToken);
        if (owner is null || owner.Role != UserRole.Administrator)
            return (null, CrmCompanyOperationResult.Failure(CrmCompanyError.Validation, "Assigned owner must be an existing administrator."));

        return (owner, null);
    }

    private static void ApplyDetails(
        DomainCrmCompany company, string name, CrmCompanyType companyType, string statusRaw, string priorityRaw,
        string? website, string? domain, string? industry, string? country, string? city, string? address,
        string? phone, string? email, Guid? assignedOwnerId, string? leadSource, string? notes)
    {
        if (!Enum.TryParse<CrmLifecycleStatus>(statusRaw, true, out var status))
            status = CrmLifecycleStatus.NewProspect;
        if (!Enum.TryParse<CrmPriority>(priorityRaw, true, out var priority))
            priority = CrmPriority.Normal;

        company.UpdateDetails(name, companyType, website, domain, industry, country, city, address, phone, email,
            status, priority, assignedOwnerId, leadSource, notes);
    }

    private static CrmCompanyResponse ToResponse(DomainCrmCompany company, DomainUser? owner) => new()
    {
        Id = company.Id,
        Name = company.Name,
        CompanyType = company.CompanyType.ToString(),
        Website = company.Website,
        Domain = company.Domain,
        Industry = company.Industry,
        Country = company.Country,
        City = company.City,
        Address = company.Address,
        Phone = company.Phone,
        Email = company.Email,
        Status = company.Status.ToString(),
        Priority = company.Priority.ToString(),
        AssignedOwner = owner is null
            ? null
            : new CrmOwnerSummary { Id = owner.Id, FirstName = owner.FirstName, LastName = owner.LastName, Email = owner.Email },
        LeadSource = company.LeadSource,
        LastContactedAt = company.LastContactedAt,
        NextFollowUpAt = company.NextFollowUpAt,
        Notes = company.Notes,
        IsArchived = company.IsArchived,
        CreatedAt = company.CreatedAt,
        UpdatedAt = company.UpdatedAt
    };
}
