namespace LimousineBooking.Application.Crm;

/// <summary>Response for GET /api/admin/users/administrators — populates CRM owner-assignment dropdowns.</summary>
public class AdministratorSummaryResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
