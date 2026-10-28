namespace LimousineBooking.Application.Crm;

/// <summary>Body for POST /api/admin/crm/companies/{id}/contacts and POST /api/admin/crm/deals/{id}/contacts.</summary>
public class LinkCrmContactRequest
{
    public Guid ContactId { get; set; }
    public bool IsPrimary { get; set; }
}
