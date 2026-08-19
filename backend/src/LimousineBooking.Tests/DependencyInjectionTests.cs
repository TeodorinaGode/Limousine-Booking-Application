using LimousineBooking.Application;
using LimousineBooking.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimousineBooking.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructure_RegistersDbContextAndJwtSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:Key"] = "test-key"
            })
            .Build();

        var result = services.AddInfrastructure(configuration);

        Assert.Same(services, result);
        Assert.Contains(services, d => d.ServiceType.Name == "ApplicationDbContext");
    }
}
