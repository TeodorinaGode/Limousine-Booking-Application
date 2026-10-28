using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A potential B2B business opportunity (e.g. an annual corporate chauffeur
/// contract) tied to one <see cref="CrmCompany"/>. Entirely separate from the
/// operational <see cref="Booking"/> workflow — winning a deal never creates
/// a booking automatically.
/// </summary>
public class CrmDeal : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public string? Description { get; private set; }
    public string? DealType { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = "CHF";
    public Guid PipelineId { get; private set; }
    public Guid StageId { get; private set; }
    public int Probability { get; private set; }
    public DateOnly? ExpectedCloseDate { get; private set; }
    public Guid? OwnerId { get; private set; }
    public string? NextAction { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? LossReason { get; private set; }
    public string? Notes { get; private set; }
    public bool IsArchived { get; private set; }

    public CrmCompany? Company { get; private set; }
    public CrmPipeline? Pipeline { get; private set; }
    public CrmPipelineStage? Stage { get; private set; }
    public User? Owner { get; private set; }
    public ICollection<CrmDealContact> DealContacts { get; private set; } = new List<CrmDealContact>();
    public ICollection<CrmActivity> Activities { get; private set; } = new List<CrmActivity>();
    public ICollection<CrmTask> Tasks { get; private set; } = new List<CrmTask>();

    private CrmDeal()
    {
    }

    public CrmDeal(string name, Guid companyId, Guid pipelineId, Guid stageId, decimal value, string currency)
    {
        ValidateName(name);
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required.", nameof(companyId));
        if (pipelineId == Guid.Empty)
            throw new ArgumentException("PipelineId is required.", nameof(pipelineId));
        if (stageId == Guid.Empty)
            throw new ArgumentException("StageId is required.", nameof(stageId));
        ValidateValue(value);
        ValidateCurrency(currency);

        Name = name.Trim();
        CompanyId = companyId;
        PipelineId = pipelineId;
        StageId = stageId;
        Value = value;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public void UpdateDetails(
        string name,
        string? description,
        string? dealType,
        decimal value,
        string currency,
        DateOnly? expectedCloseDate,
        Guid? ownerId,
        string? nextAction,
        string? notes)
    {
        ValidateName(name);
        ValidateValue(value);
        ValidateCurrency(currency);

        Name = name.Trim();
        Description = Normalize(description);
        DealType = Normalize(dealType);
        Value = value;
        Currency = currency.Trim().ToUpperInvariant();
        ExpectedCloseDate = expectedCloseDate;
        OwnerId = ownerId;
        NextAction = Normalize(nextAction);
        Notes = Normalize(notes);
    }

    /// <summary>
    /// Moves the deal to a new stage within its pipeline. Entering a Lost
    /// stage requires a reason; entering any terminal stage (Won or Lost)
    /// stamps ClosedAt, and moving back out of one (re-opening the deal)
    /// clears both ClosedAt and LossReason.
    /// </summary>
    public void MoveToStage(Guid stageId, int probability, bool isWon, bool isLost, string? lossReason)
    {
        if (stageId == Guid.Empty)
            throw new ArgumentException("StageId is required.", nameof(stageId));
        if (isLost && string.IsNullOrWhiteSpace(lossReason))
            throw new ArgumentException("A loss reason is required when moving a deal into a Lost stage.", nameof(lossReason));
        if (probability < 0 || probability > 100)
            throw new ArgumentException("Probability must be between 0 and 100.", nameof(probability));

        StageId = stageId;
        Probability = probability;

        if (isWon || isLost)
        {
            ClosedAt = DateTime.UtcNow;
            LossReason = isLost ? lossReason!.Trim() : null;
        }
        else
        {
            ClosedAt = null;
            LossReason = null;
        }
    }

    public void SetProbability(int probability)
    {
        if (probability < 0 || probability > 100)
            throw new ArgumentException("Probability must be between 0 and 100.", nameof(probability));

        Probability = probability;
    }

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
    }

    private static void ValidateValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative.", nameof(value));
    }

    private static void ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
