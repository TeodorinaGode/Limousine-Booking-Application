namespace LimousineBooking.Application.Account;

/// <summary>GET/PUT /api/account/preferences — the only preference exposed so far is language (Prompt 16).</summary>
public class AccountPreferencesResponse
{
    /// <summary>Null if the user has never saved a preference — the frontend then falls back to the browser's language.</summary>
    public string? LanguageCode { get; set; }
}
