namespace LimousineBooking.Application.Crm;

public class CreateCrmContactRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string Status { get; set; } = "NewProspect";
    public Guid? AssignedOwnerId { get; set; }
    public string? Notes { get; set; }

    /// <summary>Optional — if given, the new contact is linked to this company immediately.</summary>
    public Guid? CompanyId { get; set; }
    public bool IsPrimaryAtCompany { get; set; }
}
