using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainCrmCompanyContact = LimousineBooking.Domain.Entities.CrmCompanyContact;
using DomainCrmContact = LimousineBooking.Domain.Entities.CrmContact;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Application.Crm;

public class CrmContactService : ICrmContactService
{
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmEmailMessageRepository _emailMessageRepository;

    public CrmContactService(
        ICrmContactRepository contactRepository,
        ICrmCompanyRepository companyRepository,
        IUserRepository userRepository,
        ICrmActivityRepository activityRepository,
        ICrmEmailMessageRepository emailMessageRepository)
    {
        _contactRepository = contactRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _activityRepository = activityRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<PagedResult<CrmContactResponse>> SearchAsync(CrmContactSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _contactRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<CrmContactResponse>
        {
            Items = items.Select(c => ToResponse(c, c.AssignedOwner)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmContactDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetDetailByIdAsync(id, cancellationToken);
        if (contact is null)
            return null;

        var recentActivities = await _activityRepository.GetRecentForAsync(null, id, null, 10, cancellationToken);

        return new CrmContactDetailResponse
        {
            RecentActivities = await CrmActivityResponseMapper.ToResponsesAsync(recentActivities, _emailMessageRepository, cancellationToken),
            Contact = ToResponse(contact, contact.AssignedOwner),
            Companies = contact.CompanyContacts
                .Select(cc => new CrmCompanySummary
                {
                    Id = cc.Company!.Id,
                    Name = cc.Company.Name,
                    CompanyType = cc.Company.CompanyType.ToString()
                })
                .ToList(),
            Deals = contact.DealContacts
                .Where(dc => dc.Deal is not null)
                .Select(dc => new CrmDealSummary
                {
                    Id = dc.Deal!.Id,
                    Name = dc.Deal.Name,
                    Value = dc.Deal.Value,
                    Currency = dc.Deal.Currency,
                    StageId = dc.Deal.StageId,
                    StageName = dc.Deal.Stage?.Name ?? string.Empty,
                    Probability = dc.Deal.Probability,
                    IsWon = dc.Deal.Stage?.IsWon ?? false,
                    IsLost = dc.Deal.Stage?.IsLost ?? false
                })
                .ToList(),
            OpenTasks = contact.Tasks
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

    public async Task<CrmContactOperationResult> CreateAsync(CreateCrmContactRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        if (await _contactRepository.HasDuplicateEmailAsync(email, null, cancellationToken))
            return CrmContactOperationResult.Failure(CrmContactError.Duplicate, "A contact with this email already exists.");

        var ownerCheck = await ValidateOwnerAsync(request.AssignedOwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        if (request.CompanyId.HasValue && await _companyRepository.GetByIdAsync(request.CompanyId.Value, cancellationToken) is null)
            return CrmContactOperationResult.Failure(CrmContactError.Validation, "Company not found.");

        DomainCrmContact contact;
        try
        {
            contact = new DomainCrmContact(request.FirstName, request.LastName, email);
            ApplyDetails(contact, request.FirstName, request.LastName, email, request.Status, request.JobTitle,
                request.Phone, request.PreferredContactMethod, request.AssignedOwnerId, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmContactOperationResult.Failure(CrmContactError.Validation, ex.Message);
        }

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _contactRepository.SaveChangesAsync(cancellationToken);

        if (request.CompanyId.HasValue)
            await _companyRepository.AddCompanyContactAsync(new DomainCrmCompanyContact(request.CompanyId.Value, contact.Id, request.IsPrimaryAtCompany), cancellationToken);
        await _contactRepository.SaveChangesAsync(cancellationToken);

        return CrmContactOperationResult.Success(ToResponse(contact, ownerCheck.Owner));
    }

    public async Task<CrmContactOperationResult> UpdateAsync(Guid id, UpdateCrmContactRequest request, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
        if (contact is null)
            return CrmContactOperationResult.Failure(CrmContactError.NotFound, "Contact not found.");

        var email = request.Email.Trim();

        if (await _contactRepository.HasDuplicateEmailAsync(email, id, cancellationToken))
            return CrmContactOperationResult.Failure(CrmContactError.Duplicate, "A contact with this email already exists.");

        var ownerCheck = await ValidateOwnerAsync(request.AssignedOwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        try
        {
            ApplyDetails(contact, request.FirstName, request.LastName, email, request.Status, request.JobTitle,
                request.Phone, request.PreferredContactMethod, request.AssignedOwnerId, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmContactOperationResult.Failure(CrmContactError.Validation, ex.Message);
        }

        await _contactRepository.SaveChangesAsync(cancellationToken);

        return CrmContactOperationResult.Success(ToResponse(contact, ownerCheck.Owner));
    }

    public async Task<CrmContactOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
        if (contact is null)
            return CrmContactOperationResult.Failure(CrmContactError.NotFound, "Contact not found.");

        if (isArchived) contact.Archive(); else contact.Unarchive();

        await _contactRepository.SaveChangesAsync(cancellationToken);

        return CrmContactOperationResult.Success(ToResponse(contact, contact.AssignedOwner));
    }

    public async Task<CrmContactOperationResult> RecordContactedAsync(Guid id, DateTime contactedAtUtc, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
        if (contact is null)
            return CrmContactOperationResult.Failure(CrmContactError.NotFound, "Contact not found.");

        contact.RecordContact(contactedAtUtc);
        await _contactRepository.SaveChangesAsync(cancellationToken);

        return CrmContactOperationResult.Success(ToResponse(contact, contact.AssignedOwner));
    }

    public async Task<CrmContactOperationResult> ScheduleFollowUpAsync(Guid id, DateTime? followUpAtUtc, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(id, cancellationToken);
        if (contact is null)
            return CrmContactOperationResult.Failure(CrmContactError.NotFound, "Contact not found.");

        contact.ScheduleFollowUp(followUpAtUtc);
        await _contactRepository.SaveChangesAsync(cancellationToken);

        return CrmContactOperationResult.Success(ToResponse(contact, contact.AssignedOwner));
    }

    private async Task<(DomainUser? Owner, CrmContactOperationResult? Error)> ValidateOwnerAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (!ownerId.HasValue)
            return (null, null);

        var owner = await _userRepository.GetByIdAsync(ownerId.Value, cancellationToken);
        if (owner is null || owner.Role != UserRole.Administrator)
            return (null, CrmContactOperationResult.Failure(CrmContactError.Validation, "Assigned owner must be an existing administrator."));

        return (owner, null);
    }

    private static void ApplyDetails(
        DomainCrmContact contact, string firstName, string lastName, string email, string statusRaw,
        string? jobTitle, string? phone, string? preferredContactMethod, Guid? assignedOwnerId, string? notes)
    {
        if (!Enum.TryParse<CrmLifecycleStatus>(statusRaw, true, out var status))
            status = CrmLifecycleStatus.NewProspect;

        contact.UpdateDetails(firstName, lastName, email, jobTitle, phone, preferredContactMethod, status, assignedOwnerId, notes);
    }

    private static CrmContactResponse ToResponse(DomainCrmContact contact, DomainUser? owner) => new()
    {
        Id = contact.Id,
        FirstName = contact.FirstName,
        LastName = contact.LastName,
        Email = contact.Email,
        JobTitle = contact.JobTitle,
        Phone = contact.Phone,
        PreferredContactMethod = contact.PreferredContactMethod,
        Status = contact.Status.ToString(),
        AssignedOwner = owner is null
            ? null
            : new CrmOwnerSummary { Id = owner.Id, FirstName = owner.FirstName, LastName = owner.LastName, Email = owner.Email },
        LastContactedAt = contact.LastContactedAt,
        NextFollowUpAt = contact.NextFollowUpAt,
        Notes = contact.Notes,
        IsArchived = contact.IsArchived,
        CreatedAt = contact.CreatedAt,
        UpdatedAt = contact.UpdatedAt
    };
}
