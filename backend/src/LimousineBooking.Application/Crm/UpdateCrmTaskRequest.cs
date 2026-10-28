namespace LimousineBooking.Application.Crm;

public class UpdateCrmTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }
    public Guid AssignedUserId { get; set; }
    public string Priority { get; set; } = "Normal";
    public Guid? CompanyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
}
