namespace LimousineBooking.Application.Crm;

public class CrmCompanyDetailResponse
{
    public CrmCompanyResponse Company { get; set; } = new();
    public IReadOnlyList<CrmContactSummary> Contacts { get; set; } = Array.Empty<CrmContactSummary>();
    public IReadOnlyList<CrmDealSummary> Deals { get; set; } = Array.Empty<CrmDealSummary>();
    public IReadOnlyList<CrmTaskSummary> OpenTasks { get; set; } = Array.Empty<CrmTaskSummary>();
    public IReadOnlyList<CrmActivityResponse> RecentActivities { get; set; } = Array.Empty<CrmActivityResponse>();
}
