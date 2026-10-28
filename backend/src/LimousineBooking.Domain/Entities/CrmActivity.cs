using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// An append-only logged interaction (call, email, meeting, note) with a
/// company, contact, and/or deal. Never updated or deleted once created —
/// same posture as <see cref="ContactMessage"/>.
/// </summary>
public class CrmActivity : AuditableEntity
{
    public CrmActivityType ActivityType { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? DealId { get; private set; }
    public string? Outcome { get; private set; }

    /// <summary>
    /// Stage 2A addition: set when this activity represents a real synchronized
    /// or sent email, so the timeline can deep-link to the full conversation.
    /// Null for every Stage 1 activity type and for manually-logged
    /// Call/Meeting/etc. rows. The sync processor and the send flow are the
    /// only two writers, and each sets this at most once per CrmEmailMessage
    /// — which is what prevents a sent-then-synced-back message from ever
    /// producing two timeline rows.
    /// </summary>
    public Guid? EmailMessageId { get; private set; }

    public User? User { get; private set; }
    public CrmCompany? Company { get; private set; }
    public CrmContact? Contact { get; private set; }
    public CrmDeal? Deal { get; private set; }

    private CrmActivity()
    {
    }

    public CrmActivity(
        CrmActivityType activityType,
        string subject,
        DateTime occurredAtUtc,
        Guid userId,
        Guid? companyId,
        Guid? contactId,
        Guid? dealId,
        string? description,
        string? outcome,
        Guid? emailMessageId = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        // A manually-logged activity (Call/Meeting/Note/etc.) must be deliberately
        // tied to a record. A synced or sent email is exempt — it already carries
        // its own identity via EmailMessageId, and a freshly-synced message from
        // an unrecognized sender legitimately has no CRM association yet (one can
        // be added later from the conversation view without losing this row).
        if (emailMessageId is null && companyId is null && contactId is null && dealId is null)
            throw new ArgumentException("An activity must be associated with at least one of: company, contact, deal.", nameof(companyId));

        ActivityType = activityType;
        Subject = subject.Trim();
        OccurredAt = occurredAtUtc;
        UserId = userId;
        CompanyId = companyId;
        ContactId = contactId;
        DealId = dealId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome.Trim();
        EmailMessageId = emailMessageId;
    }
}
