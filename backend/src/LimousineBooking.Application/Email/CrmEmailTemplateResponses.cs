namespace LimousineBooking.Application.Email;

public class CrmEmailTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCrmEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Language { get; set; }
}

public class UpdateCrmEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Language { get; set; }
}

public class CrmEmailTemplateSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;

    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool IncludeArchived { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}

/// <summary>
/// The result of substituting a template's approved {{...}} variables
/// against real Company/Contact/Deal/Owner records. UnresolvedVariables
/// lists every token that had no value to substitute (an unknown token, or
/// a known one whose context record wasn't supplied) — the frontend must
/// surface these and block sending until the composer either fills them in
/// or the user removes them, never send a template with a literal
/// "{{contact.firstName}}" still in it.
/// </summary>
public class CrmEmailTemplateRenderResult
{
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public IReadOnlyList<string> UnresolvedVariables { get; set; } = Array.Empty<string>();
}
