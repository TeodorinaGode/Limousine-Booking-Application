using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class EmailConnectionServiceTests
{
    private readonly Mock<IEmailProviderConnectionRepository> _connectionRepository = new();
    private readonly Mock<IEmailSyncCheckpointRepository> _checkpointRepository = new();
    private readonly Mock<IEmailProviderClientFactory> _clientFactory = new();
    private readonly Mock<IEmailProviderClient> _providerClient = new();
    private readonly Mock<IOAuthStateService> _stateService = new();
    private readonly Mock<ITokenEncryptionService> _tokenEncryption = new();

    private GmailOAuthSettings _settings = new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUri = "https://localhost:5249/api/admin/crm/email/connection/callback",
        FrontendSettingsUrl = "https://localhost:5173/admin/crm/inbox/settings"
    };

    public EmailConnectionServiceTests()
    {
        _clientFactory.Setup(f => f.GetClient(EmailProvider.Gmail)).Returns(_providerClient.Object);
    }

    private EmailConnectionService CreateService() => new(
        _connectionRepository.Object, _checkpointRepository.Object, _clientFactory.Object,
        _stateService.Object, _tokenEncryption.Object, Options.Create(_settings),
        Mock.Of<ILogger<EmailConnectionService>>());

    [Fact]
    public async Task BuildAuthorizationUrlAsync_WhenNotConfigured_Throws()
    {
        _settings = new GmailOAuthSettings(); // empty ClientId/ClientSecret/RedirectUri
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<EmailProviderNotConfiguredException>(() => CreateService().BuildAuthorizationUrlAsync(userId));
    }

    [Fact]
    public async Task BuildAuthorizationUrlAsync_WhenConfigured_ReturnsTheProviderBuiltUrl()
    {
        var userId = Guid.NewGuid();
        _stateService.Setup(s => s.CreateState(userId)).Returns("signed-state");
        _providerClient.Setup(c => c.BuildAuthorizationUrl("signed-state", _settings.RedirectUri)).Returns("https://accounts.google.com/o/oauth2/v2/auth?...");

        var result = await CreateService().BuildAuthorizationUrlAsync(userId);

        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth?...", result.AuthorizationUrl);
    }

    [Fact]
    public async Task HandleOAuthCallbackAsync_WithProviderError_RedirectsWithFailure()
    {
        var redirect = await CreateService().HandleOAuthCallbackAsync(code: null, state: null, providerError: "access_denied");

        Assert.StartsWith(_settings.FrontendSettingsUrl, redirect);
        Assert.Contains("connected=false", redirect);
    }

    [Fact]
    public async Task HandleOAuthCallbackAsync_WithInvalidState_RedirectsWithFailure_AndNeverExchangesTheCode()
    {
        _stateService.Setup(s => s.TryValidateState("bad-state", out It.Ref<Guid>.IsAny)).Returns(false);

        var redirect = await CreateService().HandleOAuthCallbackAsync(code: "some-code", state: "bad-state", providerError: null);

        Assert.Contains("connected=false", redirect);
        _providerClient.Verify(c => c.ExchangeCodeForTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleOAuthCallbackAsync_WhenGoogleReturnsNoRefreshToken_RedirectsWithFailure_AndDoesNotPersistAConnection()
    {
        var userId = Guid.NewGuid();
        SetupValidState(userId);
        _providerClient.Setup(c => c.ExchangeCodeForTokensAsync("auth-code", _settings.RedirectUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailProviderTokenResult { AccessToken = "access", RefreshToken = null, EmailAddress = "sales@roi-limousinen.ch" });

        var redirect = await CreateService().HandleOAuthCallbackAsync("auth-code", "good-state", null);

        Assert.Contains("connected=false", redirect);
        _connectionRepository.Verify(r => r.AddAsync(It.IsAny<EmailProviderConnection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleOAuthCallbackAsync_OnSuccess_PersistsAnEncryptedConnectionAndACheckpoint_AndRedirectsWithSuccess()
    {
        var userId = Guid.NewGuid();
        SetupValidState(userId);
        _providerClient.Setup(c => c.ExchangeCodeForTokensAsync("auth-code", _settings.RedirectUri, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailProviderTokenResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                AccessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                EmailAddress = "sales@roi-limousinen.ch"
            });
        _tokenEncryption.Setup(e => e.Encrypt("refresh-token")).Returns("encrypted-refresh-token");

        var redirect = await CreateService().HandleOAuthCallbackAsync("auth-code", "good-state", null);

        Assert.Contains("connected=true", redirect);
        _connectionRepository.Verify(r => r.AddAsync(
            It.Is<EmailProviderConnection>(c => c.EmailAddress == "sales@roi-limousinen.ch" && c.EncryptedRefreshToken == "encrypted-refresh-token"),
            It.IsAny<CancellationToken>()), Times.Once);
        _checkpointRepository.Verify(r => r.AddAsync(It.IsAny<EmailSyncCheckpoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNoConnectionExists_DoesNothing()
    {
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((EmailProviderConnection?)null);

        await CreateService().DisconnectAsync();

        _connectionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectAsync_RevokesAtTheProvider_AndClearsLocalTokensEvenIfRevokeFails()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted", "access", DateTime.UtcNow.AddHours(1), Guid.NewGuid());
        _connectionRepository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _tokenEncryption.Setup(e => e.Decrypt("encrypted")).Returns("decrypted-refresh-token");
        _providerClient.Setup(c => c.RevokeAsync("decrypted-refresh-token", It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("network error"));

        await CreateService().DisconnectAsync();

        Assert.Equal(EmailConnectionStatus.Disconnected, connection.Status);
        Assert.Null(connection.EncryptedRefreshToken);
        _connectionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupValidState(Guid userId) =>
        _stateService.Setup(s => s.TryValidateState("good-state", out userId)).Returns(true);
}
