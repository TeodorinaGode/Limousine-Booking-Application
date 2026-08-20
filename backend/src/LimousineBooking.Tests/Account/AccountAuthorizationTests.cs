using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LimousineBooking.Tests.Account;

/// <summary>
/// Verifies [Authorize] (any authenticated role) on /api/account/preferences — 401 is
/// produced by middleware before the request reaches a controller/database, so this
/// runs against a "Testing" host with no PostgreSQL required, matching every other
/// *AuthorizationTests class in this suite. Unlike the Admin/Driver-scoped controllers,
/// there is no role-based 403 case here — both roles use this endpoint identically.
/// </summary>
public class AccountAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:SecretKey"] = "test-only-secret-key-32-bytes-minimum!!",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60"
                });
            });
        });
    }

    [Theory]
    [InlineData("GET", "/api/account/preferences")]
    [InlineData("PUT", "/api/account/preferences")]
    public async Task UnauthenticatedRequest_IsRejected(string method, string path)
    {
        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
