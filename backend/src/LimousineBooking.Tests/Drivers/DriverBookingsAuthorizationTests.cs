using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Drivers;

/// <summary>
/// Verifies [Authorize(Roles = "Driver")] on /api/driver/dashboard, /api/driver/bookings,
/// and /api/driver/profile. Like the other *AuthorizationTests, 401/403 are produced by
/// middleware before the request reaches a controller/database, so these run against a
/// "Testing" host with no PostgreSQL required.
/// </summary>
public class DriverBookingsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DriverBookingsAuthorizationTests(WebApplicationFactory<Program> factory)
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
    [InlineData("GET", "/api/driver/dashboard")]
    [InlineData("GET", "/api/driver/profile")]
    [InlineData("GET", "/api/driver/bookings")]
    [InlineData("GET", "/api/driver/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/start")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/pickup")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/complete")]
    public async Task UnauthenticatedRequest_IsRejected(string method, string path)
    {
        var response = await CreateAuthorizedClient(null).SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/driver/dashboard")]
    [InlineData("GET", "/api/driver/profile")]
    [InlineData("GET", "/api/driver/bookings")]
    [InlineData("GET", "/api/driver/bookings/00000000-0000-0000-0000-000000000000")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/start")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/pickup")]
    [InlineData("POST", "/api/driver/bookings/00000000-0000-0000-0000-000000000000/complete")]
    public async Task AdministratorToken_CannotAccessDriverBookingEndpoints(string method, string path)
    {
        var client = CreateAuthorizedClient(CreateToken(UserRole.Administrator));

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
