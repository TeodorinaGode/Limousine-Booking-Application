namespace LimousineBooking.Application.Crm;

/// <summary>Body for POST /api/admin/crm/deals/{id}/contacts.</summary>
public class LinkCrmDealContactRequest
{
    public Guid ContactId { get; set; }
}
