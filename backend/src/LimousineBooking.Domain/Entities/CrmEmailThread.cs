using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>A synchronized Gmail thread. Company/Contact/Deal associations are many-to-many via the Crm*ThreadCompany/Contact/Deal join entities, mirroring CrmCompanyContact's shape exactly.</summary>
public class CrmEmailThread : AuditableEntity
{
    public Guid ConnectionId { get; private set; }
    public string ProviderThreadId { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public DateTime LastMessageAt { get; private set; }
    public bool HasUnread { get; private set; }

    public ICollection<CrmEmailMessage> Messages { get; private set; } = new List<CrmEmailMessage>();
    public ICollection<CrmEmailThreadCompany> ThreadCompanies { get; private set; } = new List<CrmEmailThreadCompany>();
    public ICollection<CrmEmailThreadContact> ThreadContacts { get; private set; } = new List<CrmEmailThreadContact>();
    public ICollection<CrmEmailThreadDeal> ThreadDeals { get; private set; } = new List<CrmEmailThreadDeal>();

    private CrmEmailThread()
    {
    }

    public CrmEmailThread(Guid connectionId, string providerThreadId, string subject, DateTime lastMessageAtUtc)
    {
        if (connectionId == Guid.Empty)
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (string.IsNullOrWhiteSpace(providerThreadId))
            throw new ArgumentException("ProviderThreadId is required.", nameof(providerThreadId));

        ConnectionId = connectionId;
        ProviderThreadId = providerThreadId;
        Subject = string.IsNullOrWhiteSpace(subject) ? "(no subject)" : subject.Trim();
        LastMessageAt = lastMessageAtUtc;
    }

    /// <summary>Called once per message landing in this thread (sync or send) to keep LastMessageAt/Subject/HasUnread current.</summary>
    public void RegisterMessage(DateTime occurredAtUtc, string? subject, bool isUnread)
    {
        if (occurredAtUtc >= LastMessageAt)
        {
            LastMessageAt = occurredAtUtc;
            if (!string.IsNullOrWhiteSpace(subject))
                Subject = subject.Trim();
        }

        if (isUnread)
            HasUnread = true;
    }

    public void MarkRead() => HasUnread = false;

    public void MarkUnread() => HasUnread = true;
}
