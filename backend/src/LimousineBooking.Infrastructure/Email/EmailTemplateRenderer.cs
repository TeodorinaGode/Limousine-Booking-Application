using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Common;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Loads .html template files (first line "&lt;!--SUBJECT: ...--&gt;", the rest is the
/// content body) and substitutes {{Placeholder}} tokens with plain string.Replace —
/// no templating framework. Branding (header/footer) is centralized in _Layout.html,
/// which every rendered template is wrapped in, rather than repeated per template.
/// Plain text is auto-derived from the content HTML by stripping tags, rather than
/// maintaining a second parallel set of plain-text template files.
/// </summary>
public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Regex SubjectLinePattern = new(@"^<!--SUBJECT:\s*(.*?)-->\s*", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TagPattern = new("<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex BlankLinesPattern = new(@"\n{3,}", RegexOptions.Compiled);

    private readonly string _templatesDirectory;
    private readonly EmailSettings _emailSettings;
    private readonly ConcurrentDictionary<string, string> _fileCache = new();

    public EmailTemplateRenderer(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
        _templatesDirectory = Path.Combine(AppContext.BaseDirectory, "Email", "Templates");
    }

    public RenderedEmail Render(string templateName, string languageCode, IReadOnlyDictionary<string, string> fields)
    {
        var language = SupportedLanguages.Normalize(languageCode);
        var raw = LoadFile(language, $"{templateName}.html");

        var subjectMatch = SubjectLinePattern.Match(raw);
        if (!subjectMatch.Success)
            throw new InvalidOperationException($"Email template '{templateName}' is missing its required <!--SUBJECT: ...--> first line.");

        var subjectTemplate = subjectMatch.Groups[1].Value;
        var content = raw[subjectMatch.Length..];

        var allFields = new Dictionary<string, string>(fields, StringComparer.OrdinalIgnoreCase)
        {
            ["ContactEmail"] = string.IsNullOrWhiteSpace(_emailSettings.FromEmail) ? "support@example.com" : _emailSettings.FromEmail
        };

        var subject = Substitute(subjectTemplate, allFields);
        var renderedContent = Substitute(content, allFields);

        var layout = LoadFile(language, "_Layout.html");
        var htmlBody = Substitute(layout.Replace("{{Content}}", renderedContent), allFields);

        var plainTextBody = ToPlainText(renderedContent);

        return new RenderedEmail(subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Looks in <paramref name="language"/>'s own folder first, then falls back to
    /// "en" if that specific file doesn't exist there (section 25 — a missing
    /// translation must never leave an email unsent). Only throws if the file is
    /// missing from the English folder too, since English is the one language every
    /// template is guaranteed to have.
    /// </summary>
    private string LoadFile(string language, string fileName) =>
        _fileCache.GetOrAdd($"{language}/{fileName}", _ =>
        {
            var languagePath = Path.Combine(_templatesDirectory, language, fileName);
            if (File.Exists(languagePath))
                return File.ReadAllText(languagePath);

            var fallbackPath = Path.Combine(_templatesDirectory, SupportedLanguages.Default, fileName);
            if (!File.Exists(fallbackPath))
                throw new FileNotFoundException($"Email template file not found in '{language}' or the English fallback: {fallbackPath}", fallbackPath);

            return File.ReadAllText(fallbackPath);
        });

    private static string Substitute(string template, IReadOnlyDictionary<string, string> fields)
    {
        foreach (var (key, value) in fields)
            template = template.Replace($"{{{{{key}}}}}", value);
        return template;
    }

    private static string ToPlainText(string html)
    {
        var text = TagPattern.Replace(html, "\n");
        text = WebUtility.HtmlDecode(text);
        text = WhitespacePattern.Replace(text, " ");
        text = BlankLinesPattern.Replace(text, "\n\n");
        return text.Trim();
    }
}
