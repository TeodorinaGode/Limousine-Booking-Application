namespace LimousineBooking.Application.Interfaces;

public record RenderedEmail(string Subject, string HtmlBody, string PlainTextBody);

/// <summary>
/// Renders a named template (see Infrastructure/Email/Templates/{languageCode}/*.html)
/// against a flat set of placeholder values. Deliberately simple string substitution —
/// no templating framework — per the spec's explicit "do not introduce a heavy
/// template framework unless needed."
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <param name="templateName">The template's base file name, without extension or language folder.</param>
    /// <param name="languageCode">Selects which language folder to render from — a language whose folder/file is missing falls back to English so an email is never left unsent for want of one translation (section 25).</param>
    /// <param name="fields">Placeholder values substituted into the template and layout.</param>
    RenderedEmail Render(string templateName, string languageCode, IReadOnlyDictionary<string, string> fields);
}
