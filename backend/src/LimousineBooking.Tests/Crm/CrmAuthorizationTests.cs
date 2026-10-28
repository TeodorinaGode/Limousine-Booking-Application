using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Crm;

/// <summary>
/// Verifies [Authorize(Roles = "Administrator")] on every /api/admin/crm/*
/// controller plus /api/admin/users/administrators — mirrors
/// Drivers.DriversAuthorizationTests. 401/403 happen in middleware before the
/// database is touched, so this runs against a "Testing" host with no
/// PostgreSQL required.
/// </summary>
public class CrmAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CrmAuthorizationTests(WebApplicationFactory<Program> factory)
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

    private static readonly Guid NilId = Guid.Empty;

    public static IEnumerable<object[]> ProtectedRoutes()
    {
        yield return new object[] { "GET", "/api/admin/crm/companies" };
        yield return new object[] { "GET", $"/api/admin/crm/companies/{NilId}" };
        yield return new object[] { "POST", "/api/admin/crm/companies" };
        yield return new object[] { "PUT", $"/api/admin/crm/companies/{NilId}" };
        yield return new object[] { "PUT", $"/api/admin/crm/companies/{NilId}/archive" };
        yield return new object[] { "GET", "/api/admin/crm/contacts" };
        yield return new object[] { "POST", "/api/admin/crm/contacts" };
        yield return new object[] { "GET", "/api/admin/crm/deals" };
        yield return new object[] { "GET", "/api/admin/crm/deals/board" };
        yield return new object[] { "POST", "/api/admin/crm/deals" };
        yield return new object[] { "PUT", $"/api/admin/crm/deals/{NilId}/stage" };
        yield return new object[] { "GET", "/api/admin/crm/activities" };
        yield return new object[] { "POST", "/api/admin/crm/activities" };
        yield return new object[] { "GET", "/api/admin/crm/tasks" };
        yield return new object[] { "POST", "/api/admin/crm/tasks" };
        yield return new object[] { "GET", "/api/admin/users/administrators" };
        yield return new object[] { "GET", "/api/admin/crm/dashboard" };
        yield return new object[] { "GET", "/api/admin/crm/reports/summary" };
        yield return new object[] { "GET", "/api/admin/crm/email/connection" };
        yield return new object[] { "GET", "/api/admin/crm/email/connection/authorize" };
        yield return new object[] { "POST", "/api/admin/crm/email/connection/disconnect" };
        yield return new object[] { "GET", "/api/admin/crm/email/threads" };
        yield return new object[] { "GET", $"/api/admin/crm/email/threads/{NilId}" };
        yield return new object[] { "PUT", $"/api/admin/crm/email/threads/{NilId}/read" };
        yield return new object[] { "PUT", $"/api/admin/crm/email/threads/{NilId}/unread" };
        yield return new object[] { "POST", $"/api/admin/crm/email/threads/{NilId}/companies" };
        yield return new object[] { "DELETE", $"/api/admin/crm/email/threads/{NilId}/companies/{NilId}" };
        yield return new object[] { "POST", $"/api/admin/crm/email/threads/{NilId}/contacts" };
        yield return new object[] { "DELETE", $"/api/admin/crm/email/threads/{NilId}/contacts/{NilId}" };
        yield return new object[] { "POST", $"/api/admin/crm/email/threads/{NilId}/deals" };
        yield return new object[] { "DELETE", $"/api/admin/crm/email/threads/{NilId}/deals/{NilId}" };
        yield return new object[] { "GET", $"/api/admin/crm/email/messages/{NilId}/attachments/{NilId}" };
        yield return new object[] { "PUT", $"/api/admin/crm/email/send-operations/{NilId}" };
        yield return new object[] { "GET", "/api/admin/crm/email/templates" };
        yield return new object[] { "GET", $"/api/admin/crm/email/templates/{NilId}" };
        yield return new object[] { "POST", "/api/admin/crm/email/templates" };
        yield return new object[] { "PUT", $"/api/admin/crm/email/templates/{NilId}" };
        yield return new object[] { "PUT", $"/api/admin/crm/email/templates/{NilId}/archive" };
        yield return new object[] { "GET", $"/api/admin/crm/email/templates/{NilId}/render" };
    }

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task UnauthenticatedRequest_IsRejected(string method, string path)
    {
        var response = await CreateAuthorizedClient(null).SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task DriverToken_CannotAccessCrm(string method, string path)
    {
        var client = CreateAuthorizedClient(CreateToken(UserRole.Driver));

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
