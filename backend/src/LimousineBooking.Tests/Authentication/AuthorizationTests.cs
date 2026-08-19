using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Authentication;

/// <summary>
/// Exercises the real ASP.NET Core auth/authorization pipeline (JWT bearer
/// validation, [Authorize(Roles=...)], claim mapping) against the /api/test/*
/// endpoints. Tokens are minted directly via IJwtTokenService rather than
/// through /api/auth/login, and the host runs under a "Testing" environment
/// (see Program.cs) so no database connection is required.
/// </summary>
public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task AuthenticatedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CreateAuthorizedClient(null).GetAsync("/api/test/authenticated");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithValidToken_Succeeds()
    {
        var response = await CreateAuthorizedClient(CreateToken(UserRole.Administrator)).GetAsync("/api/test/authenticated");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdministratorToken_Succeeds()
    {
        var response = await CreateAuthorizedClient(CreateToken(UserRole.Administrator)).GetAsync("/api/test/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithDriverToken_ReturnsForbidden()
    {
        var response = await CreateAuthorizedClient(CreateToken(UserRole.Driver)).GetAsync("/api/test/admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DriverEndpoint_WithDriverToken_Succeeds()
    {
        var response = await CreateAuthorizedClient(CreateToken(UserRole.Driver)).GetAsync("/api/test/driver");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DriverEndpoint_WithAdministratorToken_ReturnsForbidden()
    {
        var response = await CreateAuthorizedClient(CreateToken(UserRole.Administrator)).GetAsync("/api/test/driver");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
