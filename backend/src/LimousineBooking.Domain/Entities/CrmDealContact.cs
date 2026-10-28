using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Many-to-many link between a <see cref="CrmDeal"/> and a <see cref="CrmContact"/>.</summary>
public class CrmDealContact : AuditableEntity
{
    public Guid DealId { get; private set; }
    public Guid ContactId { get; private set; }

    public CrmDeal? Deal { get; private set; }
    public CrmContact? Contact { get; private set; }

    private CrmDealContact()
    {
    }

    public CrmDealContact(Guid dealId, Guid contactId)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required.", nameof(dealId));
        if (contactId == Guid.Empty)
            throw new ArgumentException("ContactId is required.", nameof(contactId));

        DealId = dealId;
        ContactId = contactId;
    }
}
