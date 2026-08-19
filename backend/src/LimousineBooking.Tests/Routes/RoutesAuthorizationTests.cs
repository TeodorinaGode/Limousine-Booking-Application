using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Routes;

/// <summary>
/// Verifies [Authorize(Roles = "Administrator")] on /api/admin/routes. 401/403
/// are produced by the auth/authorization middleware before the request ever
/// reaches the controller (and therefore the database), so — like
/// Authentication.AuthorizationTests — these run against a "Testing" host with
/// no PostgreSQL required. The "administrator succeeds" business-logic path is
/// covered separately by RouteServiceTests against a mocked repository.
/// </summary>
public class RoutesAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RoutesAuthorizationTests(WebApplicationFactory<Program> factory)
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

    private string CreateToken(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User($"{role}@example.com", "irrelevant-hash", "Test", role.ToString(), role);
        var driver = role == UserRole.Driver ? new Driver(user.Id, "+41791234567") : null;

        return jwtTokenService.GenerateToken(user, driver).AccessToken;
    }

    private HttpClient CreateAuthorizedClient(string? token)
    {
        var client = _factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("GET", "/api/admin/routes")]
    [InlineData("GET", "/api/admin/routes/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/admin/routes")]
    [InlineData("PUT", "/api/admin/routes/00000000-0000-0000-0000-000000000000")]
    public async Task UnauthenticatedRequest_IsRejected(string method, string path)
    {
        var response = await CreateAuthorizedClient(null).SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/routes")]
    [InlineData("POST", "/api/admin/routes")]
    [InlineData("PUT", "/api/admin/routes/00000000-0000-0000-0000-000000000000")]
    public async Task DriverToken_CannotManageRoutes(string method, string path)
    {
        var client = CreateAuthorizedClient(CreateToken(UserRole.Driver));

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
