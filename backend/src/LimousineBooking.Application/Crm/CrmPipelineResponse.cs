namespace LimousineBooking.Application.Crm;

public class CrmPipelineResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<CrmPipelineStageSummary> Stages { get; set; } = Array.Empty<CrmPipelineStageSummary>();
}

/// <summary>One Kanban column's deals, for GET /api/admin/crm/deals/board.</summary>
public class CrmPipelineBoardColumnResponse
{
    public CrmPipelineStageSummary Stage { get; set; } = new();
    public IReadOnlyList<CrmDealResponse> Deals { get; set; } = Array.Empty<CrmDealResponse>();
}

public class CrmPipelineBoardResponse
{
    public Guid PipelineId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public IReadOnlyList<CrmPipelineBoardColumnResponse> Columns { get; set; } = Array.Empty<CrmPipelineBoardColumnResponse>();
}
