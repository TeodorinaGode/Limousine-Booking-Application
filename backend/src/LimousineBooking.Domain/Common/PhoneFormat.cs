using System.Text.RegularExpressions;

namespace LimousineBooking.Domain.Common;

/// <summary>
/// Shared, deliberately loose phone format check: digits, spaces, +, -,
/// parentheses, 7-25 chars. International numbers must work, not just Swiss ones.
/// </summary>
public static class PhoneFormat
{
    private static readonly Regex Pattern = new(@"^[0-9+\-\s()]{7,25}$", RegexOptions.Compiled);

    public static bool IsValid(string phone) => Pattern.IsMatch(phone);
}
