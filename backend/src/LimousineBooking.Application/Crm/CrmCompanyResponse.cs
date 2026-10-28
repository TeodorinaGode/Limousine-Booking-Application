namespace LimousineBooking.Application.Crm;

public class CrmCompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Domain { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public CrmOwnerSummary? AssignedOwner { get; set; }
    public string? LeadSource { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
