using LimousineBooking.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class OAuthStateServiceTests
{
    private static OAuthStateService CreateService(string secretKey = "test-only-secret-key-32-bytes-minimum!!") =>
        new(Options.Create(new JwtSettings { SecretKey = secretKey }));

    [Fact]
    public void CreateState_ThenValidate_RecoversTheSameUserId()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var state = service.CreateState(userId);
        var isValid = service.TryValidateState(state, out var recoveredUserId);

        Assert.True(isValid);
        Assert.Equal(userId, recoveredUserId);
    }

    [Fact]
    public void TryValidateState_WithTamperedPayload_IsRejected()
    {
        var service = CreateService();
        var state = service.CreateState(Guid.NewGuid());
        var parts = state.Split('.');
        var tampered = $"{Guid.NewGuid()}.{parts[1]}.{parts[2]}.{parts[3]}";

        Assert.False(service.TryValidateState(tampered, out _));
    }

    [Fact]
    public void TryValidateState_SignedByADifferentKey_IsRejected()
    {
        var state = CreateService("first-secret-key-32-bytes-minimum-ok!!").CreateState(Guid.NewGuid());

        var isValid = CreateService("a-completely-different-secret-key-value").TryValidateState(state, out _);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-state")]
    [InlineData("only.two.parts")]
    public void TryValidateState_WithMalformedInput_IsRejected(string? malformed)
    {
        var service = CreateService();

        Assert.False(service.TryValidateState(malformed!, out _));
    }

}
