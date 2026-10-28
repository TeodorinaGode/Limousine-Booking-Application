using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Application.Email;

public class EmailAccessTokenProvider : IEmailAccessTokenProvider
{
    private const int RefreshBufferMinutes = 5;

    private readonly IEmailProviderClientFactory _clientFactory;
    private readonly ITokenEncryptionService _tokenEncryption;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EmailAccessTokenProvider(IEmailProviderClientFactory clientFactory, ITokenEncryptionService tokenEncryption, IDateTimeProvider dateTimeProvider)
    {
        _clientFactory = clientFactory;
        _tokenEncryption = tokenEncryption;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> GetFreshAccessTokenAsync(EmailProviderConnection connection, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        if (connection.AccessTokenExpiresAt is { } expiresAt && expiresAt > now.AddMinutes(RefreshBufferMinutes) && !string.IsNullOrWhiteSpace(connection.AccessToken))
            return connection.AccessToken;

        if (string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
            throw new EmailReauthRequiredException("No refresh token is stored for this connection.");

        var client = _clientFactory.GetClient(connection.Provider);
        var refreshToken = _tokenEncryption.Decrypt(connection.EncryptedRefreshToken);
        var result = await client.RefreshAccessTokenAsync(refreshToken, cancellationToken);

        var encryptedNewRefreshToken = result.RefreshToken is null ? null : _tokenEncryption.Encrypt(result.RefreshToken);
        connection.UpdateTokens(encryptedNewRefreshToken, result.AccessToken, result.AccessTokenExpiresAtUtc);

        return result.AccessToken;
    }
}
