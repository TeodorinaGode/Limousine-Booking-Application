using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>A follow-up task assigned to an administrator, optionally linked to a company, contact, and/or deal.</summary>
public class CrmTask : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly DueDate { get; private set; }
    public TimeOnly? DueTime { get; private set; }
    public Guid AssignedUserId { get; private set; }
    public CrmPriority Priority { get; private set; } = CrmPriority.Normal;
    public CrmTaskStatus Status { get; private set; } = CrmTaskStatus.ToDo;
    public Guid? CompanyId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? DealId { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public User? AssignedUser { get; private set; }
    public CrmCompany? Company { get; private set; }
    public CrmContact? Contact { get; private set; }
    public CrmDeal? Deal { get; private set; }

    private CrmTask()
    {
    }

    public CrmTask(
        string title,
        DateOnly dueDate,
        Guid assignedUserId,
        CrmPriority priority,
        Guid? companyId,
        Guid? contactId,
        Guid? dealId,
        string? description,
        TimeOnly? dueTime)
    {
        ValidateTitle(title);
        if (assignedUserId == Guid.Empty)
            throw new ArgumentException("AssignedUserId is required.", nameof(assignedUserId));

        Title = title.Trim();
        DueDate = dueDate;
        DueTime = dueTime;
        AssignedUserId = assignedUserId;
        Priority = priority;
        CompanyId = companyId;
        ContactId = contactId;
        DealId = dealId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void UpdateDetails(
        string title,
        string? description,
        DateOnly dueDate,
        TimeOnly? dueTime,
        Guid assignedUserId,
        CrmPriority priority,
        Guid? companyId,
        Guid? contactId,
        Guid? dealId)
    {
        ValidateTitle(title);
        if (assignedUserId == Guid.Empty)
            throw new ArgumentException("AssignedUserId is required.", nameof(assignedUserId));

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DueDate = dueDate;
        DueTime = dueTime;
        AssignedUserId = assignedUserId;
        Priority = priority;
        CompanyId = companyId;
        ContactId = contactId;
        DealId = dealId;
    }

    public void Complete()
    {
        Status = CrmTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = CrmTaskStatus.Cancelled;
        CompletedAt = null;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
    }
}
