namespace LimousineBooking.Application.Account;

/// <summary>
/// Body of PUT /api/account/preferences. <see cref="LanguageCode"/> is never
/// validated against the supported list here — an unsupported/malformed value
/// is silently normalized to English by <c>User.SetLanguage</c> (section 7), and
/// null explicitly clears the preference back to "use the browser's language".
/// </summary>
public class UpdateAccountPreferencesRequest
{
    public string? LanguageCode { get; set; }
}
