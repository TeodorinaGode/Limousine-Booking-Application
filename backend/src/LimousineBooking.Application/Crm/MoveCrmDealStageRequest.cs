namespace LimousineBooking.Application.Crm;

/// <summary>Body for PUT /api/admin/crm/deals/{id}/stage — the Kanban board's drag-and-drop endpoint.</summary>
public class MoveCrmDealStageRequest
{
    public Guid StageId { get; set; }

    /// <summary>Required when the target stage is flagged IsLost.</summary>
    public string? LossReason { get; set; }
}
