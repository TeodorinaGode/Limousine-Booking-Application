using LimousineBooking.Domain.Entities;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class RouteTests
{
    [Fact]
    public void Route_EstimatedDurationMinutes_MustBeGreaterThanZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Route("Basel", "Zurich", estimatedDurationMinutes: 0, price: 180.00m, currency: "CHF"));
    }

    [Fact]
    public void Route_Price_CannotBeNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Route("Basel", "Zurich", estimatedDurationMinutes: 60, price: -1m, currency: "CHF"));
    }
}
