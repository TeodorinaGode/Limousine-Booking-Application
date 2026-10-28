using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>A named deal pipeline (Stage 1 ships one default pipeline, seeded by migration). Stages are configurable data, not a hardcoded enum.</summary>
public class CrmPipeline : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public ICollection<CrmPipelineStage> Stages { get; private set; } = new List<CrmPipelineStage>();

    private CrmPipeline()
    {
    }

    public CrmPipeline(string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        IsDefault = isDefault;
    }
}
