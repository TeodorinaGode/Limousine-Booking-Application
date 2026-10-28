using LimousineBooking.Application.Email;

namespace LimousineBooking.Application.Interfaces;

public interface IEmailConnectionService
{
    Task<EmailConnectionStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws EmailProviderNotConfiguredException if GmailOAuth client id/secret/redirect uri are not set. The initiating admin's id is embedded in the signed state for the callback to recover — the callback itself has no bearer token.</summary>
    Task<AuthorizationUrlResponse> BuildAuthorizationUrlAsync(Guid initiatingUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute frontend URL to redirect the browser to — success or failure is encoded in its query string, never thrown past this point since the caller is a redirect-issuing controller action, not an API client.</summary>
    Task<string> HandleOAuthCallbackAsync(string? code, string? state, string? providerError, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

public class EmailProviderNotConfiguredException : Exception
{
    public EmailProviderNotConfiguredException(string message) : base(message)
    {
    }
}
