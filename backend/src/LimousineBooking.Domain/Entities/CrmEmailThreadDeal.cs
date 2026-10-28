using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>Many-to-many link between a CrmEmailThread and a CrmDeal.</summary>
public class CrmEmailThreadDeal : AuditableEntity
{
    public Guid ThreadId { get; private set; }
    public Guid DealId { get; private set; }

    public CrmEmailThread? Thread { get; private set; }
    public CrmDeal? Deal { get; private set; }

    private CrmEmailThreadDeal()
    {
    }

    public CrmEmailThreadDeal(Guid threadId, Guid dealId)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required.", nameof(dealId));

        ThreadId = threadId;
        DealId = dealId;
    }
}
