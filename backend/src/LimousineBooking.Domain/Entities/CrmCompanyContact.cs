using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Many-to-many link between a <see cref="CrmCompany"/> and a <see cref="CrmContact"/>.</summary>
public class CrmCompanyContact : AuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid ContactId { get; private set; }
    public bool IsPrimary { get; private set; }

    public CrmCompany? Company { get; private set; }
    public CrmContact? Contact { get; private set; }

    private CrmCompanyContact()
    {
    }

    public CrmCompanyContact(Guid companyId, Guid contactId, bool isPrimary = false)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required.", nameof(companyId));
        if (contactId == Guid.Empty)
            throw new ArgumentException("ContactId is required.", nameof(contactId));

        CompanyId = companyId;
        ContactId = contactId;
        IsPrimary = isPrimary;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
