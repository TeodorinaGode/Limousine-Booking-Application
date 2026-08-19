using LimousineBooking.Domain.Entities;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class VehicleTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Vehicle_PassengerCapacity_MustBeGreaterThanZero(int passengerCapacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Vehicle("BS-123456", "Mercedes-Benz", "S-Class", "Sedan", passengerCapacity));
    }
}
