namespace LimousineBooking.Application.Crm;

public class CrmContactDetailResponse
{
    public CrmContactResponse Contact { get; set; } = new();
    public IReadOnlyList<CrmCompanySummary> Companies { get; set; } = Array.Empty<CrmCompanySummary>();
    public IReadOnlyList<CrmDealSummary> Deals { get; set; } = Array.Empty<CrmDealSummary>();
    public IReadOnlyList<CrmTaskSummary> OpenTasks { get; set; } = Array.Empty<CrmTaskSummary>();
    public IReadOnlyList<CrmActivityResponse> RecentActivities { get; set; } = Array.Empty<CrmActivityResponse>();
}
