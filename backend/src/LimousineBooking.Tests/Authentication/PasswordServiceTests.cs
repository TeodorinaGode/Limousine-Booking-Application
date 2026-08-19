using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LimousineBooking.Tests.Authentication;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService = new(new PasswordHasher<User>());

    [Fact]
    public void Hash_DoesNotReturnThePlainTextPassword()
    {
        var hash = _passwordService.Hash("Test#Passw0rd!");

        Assert.NotEqual("Test#Passw0rd!", hash);
        Assert.DoesNotContain("Test#Passw0rd!", hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _passwordService.Hash("Test#Passw0rd!");

        Assert.True(_passwordService.Verify(hash, "Test#Passw0rd!"));
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _passwordService.Hash("Test#Passw0rd!");

        Assert.False(_passwordService.Verify(hash, "SomethingElse!"));
    }
}
