using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Many-to-many link between a CrmEmailThread and a CrmContact.</summary>
public class CrmEmailThreadContact : AuditableEntity
{
    public Guid ThreadId { get; private set; }
    public Guid ContactId { get; private set; }

    public CrmEmailThread? Thread { get; private set; }
    public CrmContact? Contact { get; private set; }

    private CrmEmailThreadContact()
    {
    }

    public CrmEmailThreadContact(Guid threadId, Guid contactId)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (contactId == Guid.Empty)
            throw new ArgumentException("ContactId is required.", nameof(contactId));

        ThreadId = threadId;
        ContactId = contactId;
    }
}
