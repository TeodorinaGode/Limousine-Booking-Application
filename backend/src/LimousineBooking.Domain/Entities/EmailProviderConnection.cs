using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// The CRM's one shared business mailbox connection (Stage 2A: Gmail only).
/// Any Administrator can view/manage it — role-gated like everything else in
/// this app, not owned by the connecting user. EncryptedRefreshToken is
/// encrypted at rest via ASP.NET Core Data Protection before it ever reaches
/// this entity; AccessToken is never exposed to the frontend.
/// </summary>
public class EmailProviderConnection : AuditableEntity
{
    public EmailProvider Provider { get; private set; }
    public string EmailAddress { get; private set; } = string.Empty;
    public EmailConnectionStatus Status { get; private set; } = EmailConnectionStatus.Disconnected;
    public string? EncryptedRefreshToken { get; private set; }
    public string? AccessToken { get; private set; }
    public DateTime? AccessTokenExpiresAt { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public string? LastSyncError { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private EmailProviderConnection()
    {
    }

    public EmailProviderConnection(
        EmailProvider provider, string emailAddress, string encryptedRefreshToken,
        string accessToken, DateTime accessTokenExpiresAtUtc, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            throw new ArgumentException("Email address is required.", nameof(emailAddress));
        if (!EmailFormat.IsValid(emailAddress))
            throw new ArgumentException("Email address format is invalid.", nameof(emailAddress));
        if (string.IsNullOrWhiteSpace(encryptedRefreshToken))
            throw new ArgumentException("Refresh token is required.", nameof(encryptedRefreshToken));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        Provider = provider;
        EmailAddress = emailAddress.Trim();
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAtUtc;
        CreatedByUserId = createdByUserId;
        Status = EmailConnectionStatus.Connected;
    }

    /// <summary>Refreshed access token, and a possibly-reissued refresh token — providers don't always return a new one on a refresh grant, so null here means "keep the existing one".</summary>
    public void UpdateTokens(string? encryptedRefreshToken, string accessToken, DateTime accessTokenExpiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required.", nameof(accessToken));

        if (!string.IsNullOrWhiteSpace(encryptedRefreshToken))
            EncryptedRefreshToken = encryptedRefreshToken;

        AccessToken = accessToken;
        AccessTokenExpiresAt = accessTokenExpiresAtUtc;

        if (Status is EmailConnectionStatus.ReauthRequired or EmailConnectionStatus.Error)
            Status = EmailConnectionStatus.Connected;
    }

    public void MarkSynced(DateTime syncedAtUtc)
    {
        LastSyncedAt = syncedAtUtc;
        LastSyncError = null;
        if (Status != EmailConnectionStatus.Disconnected)
            Status = EmailConnectionStatus.Connected;
    }

    public void MarkSyncFailed(string errorMessage)
    {
        LastSyncError = errorMessage;
        if (Status == EmailConnectionStatus.Connected)
            Status = EmailConnectionStatus.Error;
    }

    /// <summary>The provider rejected the refresh token (revoked/expired) — only a fresh OAuth connect can recover.</summary>
    public void MarkReauthRequired() => Status = EmailConnectionStatus.ReauthRequired;

    /// <summary>Clears all tokens — a disconnected connection can never be used to call the provider again until reconnected.</summary>
    public void Disconnect()
    {
        Status = EmailConnectionStatus.Disconnected;
        EncryptedRefreshToken = null;
        AccessToken = null;
        AccessTokenExpiresAt = null;
    }
}
