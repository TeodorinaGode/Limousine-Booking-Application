using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// One ordered stage within a <see cref="CrmPipeline"/>. IsWon/IsLost mark the
/// two terminal stages so "open pipeline" queries are a plain predicate rather
/// than fragile stage-name matching.
/// </summary>
public class CrmPipelineStage : AuditableEntity
{
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsLost { get; private set; }
    public int DefaultProbability { get; private set; }

    public CrmPipeline? Pipeline { get; private set; }

    private CrmPipelineStage()
    {
    }

    public CrmPipelineStage(Guid pipelineId, string name, int sortOrder, bool isWon, bool isLost, int defaultProbability)
    {
        if (pipelineId == Guid.Empty)
            throw new ArgumentException("PipelineId is required.", nameof(pipelineId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (isWon && isLost)
            throw new ArgumentException("A stage cannot be both Won and Lost.", nameof(isLost));
        ValidateProbability(defaultProbability);

        PipelineId = pipelineId;
        Name = name.Trim();
        SortOrder = sortOrder;
        IsWon = isWon;
        IsLost = isLost;
        DefaultProbability = defaultProbability;
    }

    private static void ValidateProbability(int probability)
    {
        if (probability < 0 || probability > 100)
            throw new ArgumentException("Probability must be between 0 and 100.", nameof(probability));
    }
}
