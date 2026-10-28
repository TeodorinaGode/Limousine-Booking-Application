namespace LimousineBooking.Application.Crm;

public class UpdateCrmDealRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DealType { get; set; }
    public decimal Value { get; set; }
    public string Currency { get; set; } = "CHF";
    public DateOnly? ExpectedCloseDate { get; set; }
    public Guid? OwnerId { get; set; }
    public string? NextAction { get; set; }
    public string? Notes { get; set; }
}
