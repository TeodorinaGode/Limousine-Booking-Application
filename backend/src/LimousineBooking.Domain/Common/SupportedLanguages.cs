namespace LimousineBooking.Domain.Common;

/// <summary>
/// The four languages this application supports (Prompt 16) — en/de/fr/it, always
/// short lowercase codes, never a display name ("German") or region-qualified tag
/// ("de-CH"). <see cref="Normalize"/> is the single place that decides what happens
/// to an unsupported/missing code: it silently falls back to English rather than
/// rejecting the request, since a bad language preference should never block a
/// booking or a login — it only affects which language the customer/user sees.
/// </summary>
public static class SupportedLanguages
{
    public const string Default = "en";

    public static readonly IReadOnlyCollection<string> Codes = new[] { "en", "de", "fr", "it" };

    public static bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Codes.Contains(code.Trim().ToLowerInvariant());

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Default;

        var trimmed = code.Trim().ToLowerInvariant();
        return Codes.Contains(trimmed) ? trimmed : Default;
    }
}
