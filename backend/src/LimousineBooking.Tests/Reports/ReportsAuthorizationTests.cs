using System.Net;
using System.Net.Http.Headers;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Tests.Reports;

/// <summary>
/// Verifies [Authorize(Roles = "Administrator")] on every /api/admin/reports
/// endpoint, including CSV exports (section 33/38/62). 401/403 are produced by
/// middleware before the request reaches a controller/database, so these run
/// against a "Testing" host with no PostgreSQL required.
/// </summary>
public class ReportsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportsAuthorizationTests(WebApplicationFactory<Program> factory)
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

    public static IEnumerable<object[]> ReportEndpoints => new[]
    {
        new object[] { "/api/admin/reports/summary" },
        new object[] { "/api/admin/reports/revenue-by-day" },
        new object[] { "/api/admin/reports/bookings-by-day" },
        new object[] { "/api/admin/reports/routes" },
        new object[] { "/api/admin/reports/drivers" },
        new object[] { "/api/admin/reports/vehicles" },
        new object[] { "/api/admin/reports/passengers" },
        new object[] { "/api/admin/reports/status-distribution" },
        new object[] { "/api/admin/reports/assignments" },
        new object[] { "/api/admin/reports/payments" },
        new object[] { "/api/admin/reports/unassigned" },
        new object[] { "/api/admin/reports/upcoming" },
        new object[] { "/api/admin/reports/cancellations" },
        new object[] { "/api/admin/reports/bookings/export" },
        new object[] { "/api/admin/reports/routes/export" },
        new object[] { "/api/admin/reports/drivers/export" },
        new object[] { "/api/admin/reports/vehicles/export" }
    };

    [Theory]
    [MemberData(nameof(ReportEndpoints))]
    public async Task UnauthenticatedRequest_IsRejected(string path)
    {
        var response = await CreateAuthorizedClient(null).GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ReportEndpoints))]
    public async Task DriverToken_CannotAccessReports(string path)
    {
        var client = CreateAuthorizedClient(CreateToken(UserRole.Driver));

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
