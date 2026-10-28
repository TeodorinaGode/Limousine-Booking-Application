namespace LimousineBooking.Application.Crm;

public class CrmDealResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CrmCompanySummary Company { get; set; } = new();
    public string? Description { get; set; }
    public string? DealType { get; set; }
    public decimal Value { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
    public int Probability { get; set; }
    public DateOnly? ExpectedCloseDate { get; set; }
    public CrmOwnerSummary? Owner { get; set; }
    public string? NextAction { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LossReason { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
