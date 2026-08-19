using LimousineBooking.Application.Availability;
using LimousineBooking.Application.Interfaces;
using Moq;
using DomainAvailability = LimousineBooking.Domain.Entities.DriverAvailability;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainUser = LimousineBooking.Domain.Entities.User;
using UserRole = LimousineBooking.Domain.Enums.UserRole;

namespace LimousineBooking.Tests.Availability;

public class DriverAvailabilityServiceTests
{
    private readonly Mock<IDriverAvailabilityRepository> _availabilityRepository = new();
    private readonly Mock<IDriverRepository> _driverRepository = new();

    private DriverAvailabilityService CreateService() => new(_availabilityRepository.Object, _driverRepository.Object);

    private static DomainDriver ActiveDriver()
    {
        var user = new DomainUser("driver@example.com", "hash", "Test", "Driver", UserRole.Driver);
        return new DomainDriver(user.Id, "+41791234567");
    }

    private static CreateAvailabilityRequest ValidCreateRequest() => new()
    {
        Date = new DateOnly(2026, 9, 15),
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(17, 0),
        IsAvailable = true,
        Notes = null
    };

    // ---- Create ----

    [Fact]
    public async Task Create_ForActiveDriver_Succeeds()
    {
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().CreateAsync(driver.Id, ValidCreateRequest());

        Assert.True(result.Succeeded);
        Assert.True(result.Availability!.IsAvailable);
        _availabilityRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ForInactiveDriver_IsRejected()
    {
        var driver = ActiveDriver();
        driver.Deactivate();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var result = await CreateService().CreateAsync(driver.Id, ValidCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.Validation, result.Error);
        _availabilityRepository.Verify(r => r.AddAsync(It.IsAny<DomainAvailability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithEndTimeNotAfterStartTime_IsRejected()
    {
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = ValidCreateRequest();
        request.EndTime = request.StartTime;

        var result = await CreateService().CreateAsync(driver.Id, request);

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithOverlappingPeriod_IsRejected()
    {
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().CreateAsync(driver.Id, ValidCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.Conflict, result.Error);
    }

    [Fact]
    public async Task Create_WithNonOverlappingPeriod_Succeeds()
    {
        // 08:00-12:00 and 13:00-17:00 on the same day must not be treated as a conflict.
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAvailabilityRequest
        {
            Date = new DateOnly(2026, 9, 15),
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(17, 0),
            IsAvailable = true
        };

        var result = await CreateService().CreateAsync(driver.Id, request);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Create_SupportsUnavailablePeriods()
    {
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateAvailabilityRequest
        {
            Date = new DateOnly(2026, 9, 15),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            IsAvailable = false,
            Notes = "Doctor appointment"
        };

        var result = await CreateService().CreateAsync(driver.Id, request);

        Assert.True(result.Succeeded);
        Assert.False(result.Availability!.IsAvailable);
        Assert.Equal("Doctor appointment", result.Availability.Notes);
    }

    // ---- Update / Delete / ownership ----

    [Fact]
    public async Task Update_OwnRecord_Succeeds()
    {
        var driver = ActiveDriver();
        var record = new DomainAvailability(driver.Id, new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(12, 0), true);
        _availabilityRepository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new UpdateAvailabilityRequest
        {
            Date = new DateOnly(2026, 9, 15),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            IsAvailable = true
        };

        var result = await CreateService().UpdateAsync(driver.Id, record.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal(new TimeOnly(9, 0), result.Availability!.StartTime);
    }

    [Fact]
    public async Task Update_AnotherDriversRecord_ReturnsNotFound()
    {
        var owner = ActiveDriver();
        var otherDriverId = Guid.NewGuid();
        var record = new DomainAvailability(owner.Id, new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(12, 0), true);
        _availabilityRepository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var result = await CreateService().UpdateAsync(otherDriverId, record.Id, new UpdateAvailabilityRequest
        {
            Date = record.Date,
            StartTime = record.StartTime,
            EndTime = record.EndTime,
            IsAvailable = true
        });

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_WithOverlap_IsRejected()
    {
        var driver = ActiveDriver();
        var record = new DomainAvailability(driver.Id, new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(12, 0), true);
        _availabilityRepository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().UpdateAsync(driver.Id, record.Id, new UpdateAvailabilityRequest
        {
            Date = record.Date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            IsAvailable = true
        });

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.Conflict, result.Error);
    }

    [Fact]
    public async Task Delete_OwnRecord_Succeeds()
    {
        var driver = ActiveDriver();
        var record = new DomainAvailability(driver.Id, new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(12, 0), true);
        _availabilityRepository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var result = await CreateService().DeleteAsync(driver.Id, record.Id);

        Assert.True(result.Succeeded);
        _availabilityRepository.Verify(r => r.DeleteAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_AnotherDriversRecord_ReturnsNotFound()
    {
        var owner = ActiveDriver();
        var otherDriverId = Guid.NewGuid();
        var record = new DomainAvailability(owner.Id, new DateOnly(2026, 9, 15), new TimeOnly(8, 0), new TimeOnly(12, 0), true);
        _availabilityRepository.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var result = await CreateService().DeleteAsync(otherDriverId, record.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(AvailabilityError.NotFound, result.Error);
        _availabilityRepository.Verify(r => r.DeleteAsync(It.IsAny<DomainAvailability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Current availability ----

    [Fact]
    public async Task SetCurrentAvailability_True_SetsDriverAvailable()
    {
        var driver = ActiveDriver();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var result = await CreateService().SetCurrentAvailabilityAsync(driver.Id, true);

        Assert.True(result);
        Assert.True(driver.IsAvailable);
    }

    [Fact]
    public async Task SetCurrentAvailability_False_SetsDriverUnavailable()
    {
        var driver = ActiveDriver();
        driver.SetAvailable();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);

        var result = await CreateService().SetCurrentAvailabilityAsync(driver.Id, false);

        Assert.False(result);
        Assert.False(driver.IsAvailable);
    }

    [Fact]
    public async Task CreatingSchedule_DoesNotChangeCurrentAvailability()
    {
        var driver = ActiveDriver();
        driver.SetUnavailable();
        _driverRepository.Setup(r => r.GetByIdAsync(driver.Id, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        _availabilityRepository.Setup(r => r.HasOverlapAsync(driver.Id, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await CreateService().CreateAsync(driver.Id, ValidCreateRequest());

        Assert.False(driver.IsAvailable); // Current-availability flag is untouched by schedule changes.
    }
}
