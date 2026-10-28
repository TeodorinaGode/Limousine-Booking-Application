namespace LimousineBooking.Application.Crm;

public class CrmContactResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public CrmOwnerSummary? AssignedOwner { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
