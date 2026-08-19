using System.Text.Json;
using LimousineBooking.Application.Authentication;

namespace LimousineBooking.Tests.Authentication;

public class LoginResponseShapeTests
{
    [Fact]
    public void AuthenticatedUserResponse_SerializedJson_NeverContainsPasswordHash()
    {
        var response = new LoginResponse
        {
            AccessToken = "token",
            ExpiresAt = DateTime.UtcNow,
            User = new AuthenticatedUserResponse
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                Role = "Administrator"
            }
        };

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
    }
}
