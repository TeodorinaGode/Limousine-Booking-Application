namespace LimousineBooking.Application.Crm;

public class CreateCrmDealRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string? Description { get; set; }
    public string? DealType { get; set; }
    public decimal Value { get; set; }
    public string Currency { get; set; } = "CHF";

    /// <summary>Null uses the default pipeline's first stage.</summary>
    public Guid? StageId { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public Guid? OwnerId { get; set; }
    public string? NextAction { get; set; }
    public string? Notes { get; set; }
}
