namespace LimousineBooking.Application.Crm;

/// <summary>Body for PUT .../contacted and PUT .../follow-up on Companies and Contacts.</summary>
public class RecordContactedRequest
{
    public DateTime ContactedAt { get; set; } = DateTime.UtcNow;
}

public class ScheduleFollowUpRequest
{
    public DateTime? FollowUpAt { get; set; }
}
