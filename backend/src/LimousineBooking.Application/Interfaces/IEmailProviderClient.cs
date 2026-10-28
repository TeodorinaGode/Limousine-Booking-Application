using LimousineBooking.Application.Email;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// The one interface every provider-specific email client implements —
/// GmailProviderClient (Stage 2A) is the only implementation today; a
/// Microsoft Graph client can be added in Stage 2B behind this same contract
/// without any CRM-facing service changing.
/// </summary>
public interface IEmailProviderClient
{
    EmailProvider Provider { get; }

    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<EmailProviderTokenResult> ExchangeCodeForTokensAsync(string code, string redirectUri, CancellationToken cancellationToken = default);

    Task<EmailProviderTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Best-effort revoke at the provider — callers must still clear local tokens even if this fails.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Null cursor means "no prior sync" — the implementation does one full fetch and returns a fresh cursor.</summary>
    Task<ProviderSyncPage> FetchIncrementalChangesAsync(string accessToken, string? cursor, CancellationToken cancellationToken = default);

    Task<ProviderMessageDetail> GetMessageAsync(string accessToken, string providerMessageId, CancellationToken cancellationToken = default);

    Task<ProviderSendResult> SendMessageAsync(string accessToken, OutgoingEmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>Used only to reconcile an ambiguous send outcome before ever retrying — never for general message search.</summary>
    Task<ProviderMessageDetail?> FindMessageByMessageIdHeaderAsync(string accessToken, string messageIdHeader, CancellationToken cancellationToken = default);

    Task<byte[]> GetAttachmentAsync(string accessToken, string providerMessageId, string providerAttachmentId, CancellationToken cancellationToken = default);
}

/// <summary>Resolves the right IEmailProviderClient by EmailProvider — Stage 2A only ever resolves Gmail; MicrosoftGraph throws a clear "not implemented in Stage 2A" error rather than silently misbehaving.</summary>
public interface IEmailProviderClientFactory
{
    IEmailProviderClient GetClient(EmailProvider provider);
}
