using System.Text.RegularExpressions;

namespace LimousineBooking.Domain.Common;

/// <summary>Shared, deliberately loose email format check — not full RFC 5322 validation.</summary>
public static class EmailFormat
{
    private static readonly Regex Pattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool IsValid(string email) => Pattern.IsMatch(email);
}
