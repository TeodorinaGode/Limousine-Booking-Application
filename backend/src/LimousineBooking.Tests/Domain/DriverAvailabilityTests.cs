using LimousineBooking.Domain.Entities;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class DriverAvailabilityTests
{
    [Fact]
    public void Driver_CanHaveMultipleAvailabilityRecords()
    {
        var driverId = Guid.NewGuid();

        var morning = new DriverAvailability(driverId, new DateOnly(2026, 9, 10), new TimeOnly(8, 0), new TimeOnly(12, 0), isAvailable: true);
        var afternoon = new DriverAvailability(driverId, new DateOnly(2026, 9, 10), new TimeOnly(13, 0), new TimeOnly(17, 0), isAvailable: false, notes: "Personal appointment");

        Assert.Equal(driverId, morning.DriverId);
        Assert.Equal(driverId, afternoon.DriverId);
        Assert.NotEqual(morning.Id, afternoon.Id);
    }

    [Fact]
    public void DriverAvailability_EndTime_MustBeAfterStartTime()
    {
        Assert.Throws<ArgumentException>(() =>
            new DriverAvailability(Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(12, 0), new TimeOnly(8, 0), isAvailable: true));
    }
}
