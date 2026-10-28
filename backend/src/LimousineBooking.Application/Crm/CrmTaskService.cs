using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainCrmTask = LimousineBooking.Domain.Entities.CrmTask;

namespace LimousineBooking.Application.Crm;

public class CrmTaskService : ICrmTaskService
{
    private readonly ICrmTaskRepository _taskRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmDealRepository _dealRepository;
    private readonly IUserRepository _userRepository;

    public CrmTaskService(
        ICrmTaskRepository taskRepository,
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        ICrmDealRepository dealRepository,
        IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _dealRepository = dealRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResult<CrmTaskResponse>> SearchAsync(CrmTaskSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _taskRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<CrmTaskResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmTaskOperationResult> CreateAsync(CreateCrmTaskRequest request, CancellationToken cancellationToken = default)
    {
        var assignedUser = await _userRepository.GetByIdAsync(request.AssignedUserId, cancellationToken);
        if (assignedUser is null || assignedUser.Role != UserRole.Administrator)
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, "Assigned user must be an existing administrator.");

        var associationCheck = await ValidateAssociationsAsync(request.CompanyId, request.ContactId, request.DealId, cancellationToken);
        if (associationCheck is not null)
            return associationCheck;

        if (!Enum.TryParse<CrmPriority>(request.Priority, true, out var priority))
            priority = CrmPriority.Normal;

        DomainCrmTask task;
        try
        {
            task = new DomainCrmTask(request.Title, request.DueDate, request.AssignedUserId, priority,
                request.CompanyId, request.ContactId, request.DealId, request.Description, request.DueTime);
        }
        catch (ArgumentException ex)
        {
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, ex.Message);
        }

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return CrmTaskOperationResult.Success(ToResponse(task, assignedUser));
    }

    public async Task<CrmTaskOperationResult> UpdateAsync(Guid id, UpdateCrmTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.NotFound, "Task not found.");

        var assignedUser = await _userRepository.GetByIdAsync(request.AssignedUserId, cancellationToken);
        if (assignedUser is null || assignedUser.Role != UserRole.Administrator)
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, "Assigned user must be an existing administrator.");

        var associationCheck = await ValidateAssociationsAsync(request.CompanyId, request.ContactId, request.DealId, cancellationToken);
        if (associationCheck is not null)
            return associationCheck;

        if (!Enum.TryParse<CrmPriority>(request.Priority, true, out var priority))
            priority = CrmPriority.Normal;

        try
        {
            task.UpdateDetails(request.Title, request.Description, request.DueDate, request.DueTime,
                request.AssignedUserId, priority, request.CompanyId, request.ContactId, request.DealId);
        }
        catch (ArgumentException ex)
        {
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, ex.Message);
        }

        await _taskRepository.SaveChangesAsync(cancellationToken);

        return CrmTaskOperationResult.Success(ToResponse(task, assignedUser));
    }

    public async Task<CrmTaskOperationResult> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.NotFound, "Task not found.");

        task.Complete();
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return CrmTaskOperationResult.Success(ToResponse(task));
    }

    public async Task<CrmTaskOperationResult> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.NotFound, "Task not found.");

        task.Cancel();
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return CrmTaskOperationResult.Success(ToResponse(task));
    }

    private async Task<CrmTaskOperationResult?> ValidateAssociationsAsync(Guid? companyId, Guid? contactId, Guid? dealId, CancellationToken cancellationToken)
    {
        if (companyId.HasValue && await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken) is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, "Company not found.");

        if (contactId.HasValue && await _contactRepository.GetByIdAsync(contactId.Value, cancellationToken) is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, "Contact not found.");

        if (dealId.HasValue && await _dealRepository.GetByIdAsync(dealId.Value, cancellationToken) is null)
            return CrmTaskOperationResult.Failure(CrmTaskError.Validation, "Deal not found.");

        return null;
    }

    private static CrmTaskResponse ToResponse(DomainCrmTask task) => ToResponse(task, task.AssignedUser);

    private static CrmTaskResponse ToResponse(DomainCrmTask task, Domain.Entities.User? assignedUser) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        DueTime = task.DueTime,
        AssignedUser = assignedUser is null
            ? new CrmOwnerSummary()
            : new CrmOwnerSummary { Id = assignedUser.Id, FirstName = assignedUser.FirstName, LastName = assignedUser.LastName, Email = assignedUser.Email },
        Priority = task.Priority.ToString(),
        Status = task.Status.ToString(),
        CompanyId = task.CompanyId,
        CompanyName = task.Company?.Name,
        ContactId = task.ContactId,
        ContactName = task.Contact is null ? null : $"{task.Contact.FirstName} {task.Contact.LastName}",
        DealId = task.DealId,
        DealName = task.Deal?.Name,
        CompletedAt = task.CompletedAt,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
