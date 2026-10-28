using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class EmailAccessTokenProviderTests
{
    private readonly Mock<IEmailProviderClientFactory> _clientFactory = new();
    private readonly Mock<IEmailProviderClient> _providerClient = new();
    private readonly Mock<ITokenEncryptionService> _tokenEncryption = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    public EmailAccessTokenProviderTests()
    {
        _clientFactory.Setup(f => f.GetClient(It.IsAny<EmailProvider>())).Returns(_providerClient.Object);
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
    }

    private EmailAccessTokenProvider CreateProvider() =>
        new(_clientFactory.Object, _tokenEncryption.Object, _dateTimeProvider.Object);

    [Fact]
    public async Task GetFreshAccessTokenAsync_WhenTokenIsStillValid_ReturnsItWithoutCallingTheProvider()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted", "still-valid-token", Now.AddHours(1), Guid.NewGuid());

        var result = await CreateProvider().GetFreshAccessTokenAsync(connection);

        Assert.Equal("still-valid-token", result);
        _providerClient.Verify(c => c.RefreshAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFreshAccessTokenAsync_WhenNearExpiry_RefreshesAndPersistsTheNewTokens()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh", "stale-token", Now.AddMinutes(2), Guid.NewGuid());
        _tokenEncryption.Setup(e => e.Decrypt("encrypted-refresh")).Returns("decrypted-refresh");
        _tokenEncryption.Setup(e => e.Encrypt("new-refresh")).Returns("encrypted-new-refresh");
        _providerClient.Setup(c => c.RefreshAccessTokenAsync("decrypted-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailProviderTokenResult { AccessToken = "fresh-token", RefreshToken = "new-refresh", AccessTokenExpiresAtUtc = Now.AddHours(1) });

        var result = await CreateProvider().GetFreshAccessTokenAsync(connection);

        Assert.Equal("fresh-token", result);
        Assert.Equal("fresh-token", connection.AccessToken);
        Assert.Equal("encrypted-new-refresh", connection.EncryptedRefreshToken);
    }

    [Fact]
    public async Task GetFreshAccessTokenAsync_WhenProviderDoesNotReissueARefreshToken_KeepsTheExistingOne()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh", "stale-token", Now.AddMinutes(2), Guid.NewGuid());
        _tokenEncryption.Setup(e => e.Decrypt("encrypted-refresh")).Returns("decrypted-refresh");
        _providerClient.Setup(c => c.RefreshAccessTokenAsync("decrypted-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailProviderTokenResult { AccessToken = "fresh-token", RefreshToken = null, AccessTokenExpiresAtUtc = Now.AddHours(1) });

        await CreateProvider().GetFreshAccessTokenAsync(connection);

        Assert.Equal("encrypted-refresh", connection.EncryptedRefreshToken);
    }

    [Fact]
    public async Task GetFreshAccessTokenAsync_WhenNoRefreshTokenIsStored_ThrowsReauthRequired()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh", "stale-token", Now.AddMinutes(2), Guid.NewGuid());
        connection.Disconnect(); // clears EncryptedRefreshToken/AccessToken/AccessTokenExpiresAt

        await Assert.ThrowsAsync<EmailReauthRequiredException>(() => CreateProvider().GetFreshAccessTokenAsync(connection));
    }

    [Fact]
    public async Task GetFreshAccessTokenAsync_WhenTheProviderRejectsTheRefreshToken_PropagatesReauthRequired()
    {
        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh", "stale-token", Now.AddMinutes(2), Guid.NewGuid());
        _tokenEncryption.Setup(e => e.Decrypt("encrypted-refresh")).Returns("decrypted-refresh");
        _providerClient.Setup(c => c.RefreshAccessTokenAsync("decrypted-refresh", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailReauthRequiredException("revoked"));

        await Assert.ThrowsAsync<EmailReauthRequiredException>(() => CreateProvider().GetFreshAccessTokenAsync(connection));
    }
}
