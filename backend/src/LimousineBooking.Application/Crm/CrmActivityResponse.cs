namespace LimousineBooking.Application.Crm;

public class CrmActivityResponse
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public CrmOwnerSummary LoggedBy { get; set; } = new();
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? DealId { get; set; }
    public string? DealName { get; set; }
    public string? Outcome { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set only for EmailSent/EmailReceived activities the email sync/send flow created — lets the UI deep-link to the full conversation. Null for every manually-logged activity.</summary>
    public Guid? EmailMessageId { get; set; }

    /// <summary>The conversation EmailMessageId belongs to — resolved separately since CrmActivity has no navigation to CrmEmailMessage, only the raw FK.</summary>
    public Guid? EmailThreadId { get; set; }
}
