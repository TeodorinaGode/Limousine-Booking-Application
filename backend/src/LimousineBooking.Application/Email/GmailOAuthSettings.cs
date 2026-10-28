namespace LimousineBooking.Application.Email;

/// <summary>
/// Bound from the "GmailOAuth" configuration section. ClientId/ClientSecret
/// are always empty in appsettings.json — real values come only from
/// environment variables or User Secrets (same convention as
/// PaymentSettings.SecretKey/Jwt.SecretKey). When ClientId/ClientSecret are
/// empty, the connect flow fails fast with a clear "not configured" result
/// instead of building a broken OAuth redirect — see EmailConnectionService.
/// </summary>
public class GmailOAuthSettings
{
    public const string SectionName = "GmailOAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must exactly match a redirect URI registered on the Google Cloud OAuth client — e.g. http://localhost:5249/api/admin/crm/email/connection/callback in development.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>The domain used to build each outgoing message's self-generated Message-ID header (see EmailSendOperation).</summary>
    public string OutgoingMessageIdDomain { get; set; } = "roi-limousinen.ch";

    /// <summary>Where the browser is redirected after the OAuth callback finishes (success or failure) — same "store the full absolute frontend URL in settings" convention as PaymentSettings.SuccessUrl/CancelUrl.</summary>
    public string FrontendSettingsUrl { get; set; } = "http://localhost:5173/admin/crm/inbox/settings";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(RedirectUri);
}
