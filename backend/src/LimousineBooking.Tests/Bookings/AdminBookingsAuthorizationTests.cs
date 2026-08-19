using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Bookings;

/// <summary>
/// Verifies [Authorize(Roles = "Administrator")] on /api/admin/bookings. 401/403 are
/// produced by the auth/authorization middleware before the request ever reaches the
/// controller (and therefore the database), so — like RoutesAuthorizationTests — these
/// run against a "Testing" host with no PostgreSQL required. Business-logic paths are
/// covered separately by AdminBookingServiceTests against mocked repositories.
/// </summary>
public class AdminBookingsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminBookingsAuthorizationTests(WebApplicationFactory<Program> factory)
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
    [InlineData("GET", "/api/admin/bookings")]
    [InlineData("GET", "/api/admin/bookings/dashboard")]
    [InlineData("GET", "/api/admin/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("PUT", "/api/admin/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/assign")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/auto-assign")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/cancel")]
    public async Task UnauthenticatedRequest_IsRejected(string method, string path)
    {
        var response = await CreateAuthorizedClient(null).SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/bookings")]
    [InlineData("GET", "/api/admin/bookings/dashboard")]
    [InlineData("GET", "/api/admin/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("PUT", "/api/admin/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/assign")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/auto-assign")]
    [InlineData("POST", "/api/admin/bookings/00000000-0000-0000-0000-000000000000/cancel")]
    public async Task DriverToken_CannotAccessAdminBookingApis(string method, string path)
    {
        var client = CreateAuthorizedClient(CreateToken(UserRole.Driver));

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
