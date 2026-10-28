namespace LimousineBooking.Application.Crm;

public class CrmTaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }
    public CrmOwnerSummary AssignedUser { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? DealId { get; set; }
    public string? DealName { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
