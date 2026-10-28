using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Many-to-many link between a CrmEmailThread and a CrmCompany — a conversation can involve more than one company.</summary>
public class CrmEmailThreadCompany : AuditableEntity
{
    public Guid ThreadId { get; private set; }
    public Guid CompanyId { get; private set; }

    public CrmEmailThread? Thread { get; private set; }
    public CrmCompany? Company { get; private set; }

    private CrmEmailThreadCompany()
    {
    }

    public CrmEmailThreadCompany(Guid threadId, Guid companyId)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required.", nameof(companyId));

        ThreadId = threadId;
        CompanyId = companyId;
    }
}
