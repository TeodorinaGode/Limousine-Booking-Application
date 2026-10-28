namespace LimousineBooking.Application.Crm;

/// <summary>
/// Small, reused-everywhere "subset view" DTOs with no independent identity of
/// their own — grouped in one file rather than one-class-per-file, since each
/// is only ever a nested field inside a larger detail response.
/// </summary>
public class CrmOwnerSummary
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CrmCompanySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
}

public class CrmContactSummary
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public bool IsPrimary { get; set; }
}

public class CrmDealSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int Probability { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}

public class CrmTaskSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}

public class CrmPipelineStageSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
    public int DefaultProbability { get; set; }
}
