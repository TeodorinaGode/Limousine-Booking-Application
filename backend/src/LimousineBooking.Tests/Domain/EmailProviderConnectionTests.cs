using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class EmailProviderConnectionTests
{
    private static EmailProviderConnection CreateConnected() =>
        new(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted-refresh-token", "access-token", DateTime.UtcNow.AddHours(1), Guid.NewGuid());

    [Fact]
    public void Constructor_SetsStatusToConnected()
    {
        var connection = CreateConnected();

        Assert.Equal(EmailConnectionStatus.Connected, connection.Status);
    }

    [Fact]
    public void Constructor_WithInvalidEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new EmailProviderConnection(EmailProvider.Gmail, "not-an-email", "token", "access", DateTime.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void MarkReauthRequired_ThenUpdateTokens_ReturnsToConnected()
    {
        var connection = CreateConnected();
        connection.MarkReauthRequired();

        connection.UpdateTokens(null, "new-access-token", DateTime.UtcNow.AddHours(1));

        Assert.Equal(EmailConnectionStatus.Connected, connection.Status);
    }

    [Fact]
    public void UpdateTokens_WithNullRefreshToken_KeepsExistingRefreshToken()
    {
        var connection = CreateConnected();

        connection.UpdateTokens(null, "new-access-token", DateTime.UtcNow.AddHours(1));

        Assert.Equal("encrypted-refresh-token", connection.EncryptedRefreshToken);
    }

    [Fact]
    public void MarkSyncFailed_WhenConnected_SetsErrorStatus()
    {
        var connection = CreateConnected();

        connection.MarkSyncFailed("Gmail returned 503.");

        Assert.Equal(EmailConnectionStatus.Error, connection.Status);
        Assert.Equal("Gmail returned 503.", connection.LastSyncError);
    }

    [Fact]
    public void MarkSynced_ClearsErrorAndReturnsToConnected()
    {
        var connection = CreateConnected();
        connection.MarkSyncFailed("transient error");

        connection.MarkSynced(DateTime.UtcNow);

        Assert.Equal(EmailConnectionStatus.Connected, connection.Status);
        Assert.Null(connection.LastSyncError);
    }

    [Fact]
    public void Disconnect_ClearsAllTokens()
    {
        var connection = CreateConnected();

        connection.Disconnect();

        Assert.Equal(EmailConnectionStatus.Disconnected, connection.Status);
        Assert.Null(connection.EncryptedRefreshToken);
        Assert.Null(connection.AccessToken);
        Assert.Null(connection.AccessTokenExpiresAt);
    }
}
