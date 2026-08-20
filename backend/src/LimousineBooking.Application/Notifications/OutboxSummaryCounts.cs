namespace LimousineBooking.Application.Notifications;

/// <summary>Raw counts from IOutboxRepository.GetSummaryAsync — mapped into the admin dashboard response.</summary>
public class OutboxSummaryCounts
{
    public int Pending { get; set; }
    public int Retrying { get; set; }
    public int Failed { get; set; }
    public int SentToday { get; set; }
}
