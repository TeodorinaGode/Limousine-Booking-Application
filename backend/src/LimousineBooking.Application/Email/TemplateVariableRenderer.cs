using System.Net;
using System.Text.RegularExpressions;

namespace LimousineBooking.Application.Email;

/// <summary>
/// Pure substitution engine for a template's {{token}} placeholders — no DB
/// or provider access, so it's trivially unit-testable. Every substituted
/// value is HTML-escaped (the values come from real CRM records, which can
/// contain characters like &lt;/&gt;/&amp; that must never be interpreted as
/// markup). A token with no entry in `values` (unknown, or known but its
/// context record wasn't supplied) is left exactly as written and reported
/// back so the caller can block sending rather than deliver a literal
/// "{{contact.firstName}}" to a real recipient.
/// </summary>
public static class TemplateVariableRenderer
{
    private static readonly Regex TokenPattern = new(@"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}", RegexOptions.Compiled);

    public static (string Rendered, IReadOnlyList<string> Unresolved) Render(string text, IReadOnlyDictionary<string, string> values)
    {
        var unresolved = new List<string>();

        var rendered = TokenPattern.Replace(text, match =>
        {
            var token = match.Groups[1].Value;
            if (values.TryGetValue(token, out var value))
                return WebUtility.HtmlEncode(value);

            unresolved.Add(token);
            return match.Value;
        });

        return (rendered, unresolved);
    }
}
