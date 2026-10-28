using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Email;

public class EmailConnectionService : IEmailConnectionService
{
    private readonly IEmailProviderConnectionRepository _connectionRepository;
    private readonly IEmailSyncCheckpointRepository _checkpointRepository;
    private readonly IEmailProviderClientFactory _clientFactory;
    private readonly IOAuthStateService _stateService;
    private readonly ITokenEncryptionService _tokenEncryption;
    private readonly GmailOAuthSettings _settings;
    private readonly ILogger<EmailConnectionService> _logger;

    public EmailConnectionService(
        IEmailProviderConnectionRepository connectionRepository,
        IEmailSyncCheckpointRepository checkpointRepository,
        IEmailProviderClientFactory clientFactory,
        IOAuthStateService stateService,
        ITokenEncryptionService tokenEncryption,
        IOptions<GmailOAuthSettings> settings,
        ILogger<EmailConnectionService> logger)
    {
        _connectionRepository = connectionRepository;
        _checkpointRepository = checkpointRepository;
        _clientFactory = clientFactory;
        _stateService = stateService;
        _tokenEncryption = tokenEncryption;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailConnectionStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetAsync(cancellationToken);

        return new EmailConnectionStatusResponse
        {
            IsConfigured = _settings.IsConfigured,
            IsConnected = connection?.Status == EmailConnectionStatus.Connected,
            Provider = connection?.Provider.ToString(),
            EmailAddress = connection?.EmailAddress,
            Status = connection?.Status.ToString(),
            LastSyncedAt = connection?.LastSyncedAt,
            LastSyncError = connection?.LastSyncError
        };
    }

    public Task<AuthorizationUrlResponse> BuildAuthorizationUrlAsync(Guid initiatingUserId, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new EmailProviderNotConfiguredException(
                "Email integration is not configured on the server. Set GmailOAuth:ClientId, GmailOAuth:ClientSecret, and GmailOAuth:RedirectUri (via environment variables or User Secrets) to enable it.");
        }

        var client = _clientFactory.GetClient(EmailProvider.Gmail);
        var state = _stateService.CreateState(initiatingUserId);
        var url = client.BuildAuthorizationUrl(state, _settings.RedirectUri);

        return Task.FromResult(new AuthorizationUrlResponse { AuthorizationUrl = url });
    }

    public async Task<string> HandleOAuthCallbackAsync(string? code, string? state, string? providerError, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(providerError))
            return BuildRedirect(success: false, "The authorization request was not completed.");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BuildRedirect(success: false, "The authorization callback was missing required parameters.");

        if (!_stateService.TryValidateState(state, out var initiatingUserId))
            return BuildRedirect(success: false, "The authorization request could not be verified. Please try connecting again.");

        if (!_settings.IsConfigured)
            return BuildRedirect(success: false, "Email integration is not configured on the server.");

        var client = _clientFactory.GetClient(EmailProvider.Gmail);

        EmailProviderTokenResult tokenResult;
        try
        {
            tokenResult = await client.ExchangeCodeForTokensAsync(code, _settings.RedirectUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail OAuth code exchange failed.");
            return BuildRedirect(success: false, "Google rejected the authorization request. Please try again.");
        }

        if (string.IsNullOrWhiteSpace(tokenResult.RefreshToken))
            return BuildRedirect(success: false, "Google did not return a refresh token. Disconnect any prior access at myaccount.google.com/permissions and try again.");

        if (string.IsNullOrWhiteSpace(tokenResult.EmailAddress))
            return BuildRedirect(success: false, "Could not determine the connected mailbox's address.");

        var encryptedRefreshToken = _tokenEncryption.Encrypt(tokenResult.RefreshToken);

        var connection = new EmailProviderConnection(
            EmailProvider.Gmail, tokenResult.EmailAddress, encryptedRefreshToken,
            tokenResult.AccessToken, tokenResult.AccessTokenExpiresAtUtc, initiatingUserId);

        await _connectionRepository.AddAsync(connection, cancellationToken);
        await _connectionRepository.SaveChangesAsync(cancellationToken);

        await _checkpointRepository.AddAsync(new EmailSyncCheckpoint(connection.Id), cancellationToken);
        await _checkpointRepository.SaveChangesAsync(cancellationToken);

        return BuildRedirect(success: true, null);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetAsync(cancellationToken);
        if (connection is null || connection.Status == EmailConnectionStatus.Disconnected)
            return;

        if (!string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
        {
            try
            {
                var client = _clientFactory.GetClient(connection.Provider);
                await client.RevokeAsync(_tokenEncryption.Decrypt(connection.EncryptedRefreshToken), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke the email connection at the provider — disconnecting locally regardless.");
            }
        }

        connection.Disconnect();
        await _connectionRepository.SaveChangesAsync(cancellationToken);
    }

    private string BuildRedirect(bool success, string? errorMessage)
    {
        var baseUrl = _settings.FrontendSettingsUrl;
        return success
            ? $"{baseUrl}?connected=true"
            : $"{baseUrl}?connected=false&error={Uri.EscapeDataString(errorMessage ?? "Connection failed.")}";
    }
}
