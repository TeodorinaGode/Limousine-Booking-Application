namespace LimousineBooking.Application.Crm;

public class CrmDealDetailResponse
{
    public CrmDealResponse Deal { get; set; } = new();
    public IReadOnlyList<CrmContactSummary> Contacts { get; set; } = Array.Empty<CrmContactSummary>();
    public IReadOnlyList<CrmTaskSummary> OpenTasks { get; set; } = Array.Empty<CrmTaskSummary>();
    public IReadOnlyList<CrmActivityResponse> RecentActivities { get; set; } = Array.Empty<CrmActivityResponse>();
}
