using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Ensures a connection has a non-expired access token, refreshing it (and
/// persisting the result) if needed — shared by the sync processor, the
/// inbox attachment-download path, and the send service, so the
/// refresh/reauth logic exists in exactly one place.
/// </summary>
public interface IEmailAccessTokenProvider
{
    /// <summary>Throws EmailReauthRequiredException if the connection's refresh token is missing or rejected by the provider — callers must mark the connection ReauthRequired and stop, not retry.</summary>
    Task<string> GetFreshAccessTokenAsync(EmailProviderConnection connection, CancellationToken cancellationToken = default);
}
