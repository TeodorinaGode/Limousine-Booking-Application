using LimousineBooking.Domain.Common;

namespace LimousineBooking.Domain.Entities;

/// <summary>A reusable email draft with {{variable}} personalization placeholders. Templates never send anything themselves — they're only ever inserted into the composer.</summary>
public class CrmEmailTemplate : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public string Language { get; private set; } = "en";
    public Guid CreatedByUserId { get; private set; }
    public bool IsArchived { get; private set; }

    private CrmEmailTemplate()
    {
    }

    public CrmEmailTemplate(string name, string subject, string bodyHtml, string? category, string? language, Guid createdByUserId)
    {
        ValidateCore(name, subject, bodyHtml);
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        Name = name.Trim();
        Subject = subject.Trim();
        BodyHtml = bodyHtml;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Language = Common.SupportedLanguages.Normalize(language);
        CreatedByUserId = createdByUserId;
    }

    public void UpdateDetails(string name, string subject, string bodyHtml, string? category, string? language)
    {
        ValidateCore(name, subject, bodyHtml);

        Name = name.Trim();
        Subject = subject.Trim();
        BodyHtml = bodyHtml;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Language = Common.SupportedLanguages.Normalize(language);
    }

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static void ValidateCore(string name, string subject, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(bodyHtml))
            throw new ArgumentException("Body is required.", nameof(bodyHtml));
    }
}
