using LimousineBooking.Application.Availability;
using LimousineBooking.Application.Interfaces;
using Moq;
using DomainAvailability = LimousineBooking.Domain.Entities.DriverAvailability;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainUser = LimousineBooking.Domain.Entities.User;
using UserRole = LimousineBooking.Domain.Enums.UserRole;

namespace LimousineBooking.Tests.Availability;

public class AvailabilityEvaluationServiceTests
{
    private readonly Mock<IDriverRepository> _driverRepository = new();
    private readonly Mock<IDriverAvailabilityRepository> _availabilityRepository = new();

    private AvailabilityEvaluationService CreateService() => new(_driverRepository.Object, _availabilityRepository.Object);

    private static DomainDriver ActiveDriver()
    {
        var user = new DomainUser("driver@example.com", "hash", "Test", "Driver", UserRole.Driver);
        return new DomainDriver(user.Id, "+41791234567");
    }

    [Fact]
    public async Task ActiveDriver_WithAvailableSchedule_IsAvailable()
    {
        var driver = ActiveDriver();
        var date = new DateOnly(2026, 9, 15);
        var record = new DomainAvailability(driver.Id, date, new TimeOnly(8, 0), new TimeOnly(17, 0), isAvailable: true);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.GetByDriverAsync(driver.Id, date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record });

        var result = await CreateService().IsDriverAvailableAsync(driver.Id, date, new TimeOnly(9, 0), new TimeOnly(10, 0));

        Assert.True(result);
    }

    [Fact]
    public async Task InactiveDriver_WithAvailableSchedule_IsNotAvailable()
    {
        var driver = ActiveDriver();
        driver.Deactivate();
        var date = new DateOnly(2026, 9, 15);
        var record = new DomainAvailability(driver.Id, date, new TimeOnly(8, 0), new TimeOnly(17, 0), isAvailable: true);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.GetByDriverAsync(driver.Id, date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record });

        var result = await CreateService().IsDriverAvailableAsync(driver.Id, date, new TimeOnly(9, 0), new TimeOnly(10, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task RequestedTime_OutsideAnyScheduledPeriod_IsNotAvailable()
    {
        var driver = ActiveDriver();
        var date = new DateOnly(2026, 9, 15);
        var record = new DomainAvailability(driver.Id, date, new TimeOnly(8, 0), new TimeOnly(12, 0), isAvailable: true);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.GetByDriverAsync(driver.Id, date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record });

        // Requested 14:00-15:00 falls outside the 08:00-12:00 available window.
        var result = await CreateService().IsDriverAvailableAsync(driver.Id, date, new TimeOnly(14, 0), new TimeOnly(15, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task RequestedTime_InsideUnavailablePeriod_IsNotAvailable()
    {
        var driver = ActiveDriver();
        var date = new DateOnly(2026, 9, 15);
        var available = new DomainAvailability(driver.Id, date, new TimeOnly(8, 0), new TimeOnly(17, 0), isAvailable: true);
        var unavailable = new DomainAvailability(driver.Id, date, new TimeOnly(9, 0), new TimeOnly(12, 0), isAvailable: false, notes: "Doctor appointment");

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.GetByDriverAsync(driver.Id, date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { available, unavailable });

        // 10:00-11:00 is within the overall available window but also within
        // the unavailable sub-period — the unavailable record must win.
        var result = await CreateService().IsDriverAvailableAsync(driver.Id, date, new TimeOnly(10, 0), new TimeOnly(11, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task PartialOverlapWithAvailablePeriod_IsNotSufficient()
    {
        var driver = ActiveDriver();
        var date = new DateOnly(2026, 9, 15);
        var record = new DomainAvailability(driver.Id, date, new TimeOnly(8, 0), new TimeOnly(12, 0), isAvailable: true);

        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.GetByDriverAsync(driver.Id, date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record });

        // Requested 11:00-13:00 only partially overlaps the 08:00-12:00 window.
        var result = await CreateService().IsDriverAvailableAsync(driver.Id, date, new TimeOnly(11, 0), new TimeOnly(13, 0));

        Assert.False(result);
    }

    [Fact]
    public async Task UnknownDriver_IsNotAvailable()
    {
        _driverRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainDriver?)null);

        var result = await CreateService().IsDriverAvailableAsync(Guid.NewGuid(), new DateOnly(2026, 9, 15), new TimeOnly(9, 0), new TimeOnly(10, 0));

        Assert.False(result);
    }
}
