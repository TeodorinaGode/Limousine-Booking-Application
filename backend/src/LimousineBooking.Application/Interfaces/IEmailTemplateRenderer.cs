namespace LimousineBooking.Application.Interfaces;

public record RenderedEmail(string Subject, string HtmlBody, string PlainTextBody);

/// <summary>
/// Renders a named template (see Infrastructure/Email/Templates/*.html) against a
/// flat set of placeholder values. Deliberately simple string substitution — no
/// templating framework — per the spec's explicit "do not introduce a heavy
/// template framework unless needed."
/// </summary>
public interface IEmailTemplateRenderer
{
    RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string> fields);
}
