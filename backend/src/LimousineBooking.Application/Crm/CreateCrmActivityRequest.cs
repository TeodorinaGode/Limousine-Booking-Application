namespace LimousineBooking.Application.Crm;

public class CreateCrmActivityRequest
{
    public string ActivityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? OccurredAt { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string? Outcome { get; set; }
}
