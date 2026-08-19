using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Tests.Bookings;

public class PublicBookingServiceTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IBookingReferenceGenerator> _referenceGenerator = new();
    private readonly Mock<IAutomaticAssignmentService> _automaticAssignmentService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    // 2026-09-10T08:00:00Z is 10:00 Europe/Zurich (CEST, UTC+2) — chosen so lead-time
    // math below (120-minute default) lands on clean, easy-to-read local clock times.
    private static readonly DateTime FixedUtcNow = new(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

    public PublicBookingServiceTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(FixedUtcNow);
        _referenceGenerator.Setup(g => g.GenerateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("LM-20260910-123456");
        // Automatic assignment itself is covered by AutomaticAssignmentServiceTests —
        // here it's a no-op so these tests stay focused on booking-creation validation.
        _automaticAssignmentService.Setup(s => s.AssignBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private PublicBookingService CreateService(BookingSettings? settings = null) => new(
        _routeRepository.Object,
        _bookingRepository.Object,
        _referenceGenerator.Object,
        _automaticAssignmentService.Object,
        _dateTimeProvider.Object,
        Options.Create(settings ?? new BookingSettings()));

    private static DomainRoute ActiveRoute() => new("Basel", "Zurich", 60, 180.00m, "CHF");

    private static CreateBookingRequest ValidRequest(Guid routeId, DateOnly? bookingDate = null, TimeOnly? pickupTime = null) => new()
    {
        RouteId = routeId,
        BookingDate = bookingDate ?? new DateOnly(2026, 9, 10),
        PickupTime = pickupTime ?? new TimeOnly(14, 0),
        PickupAddress = "Bahnhofplatz 1, Basel",
        PassengerCount = 2,
        CustomerFirstName = "Jane",
        CustomerLastName = "Doe",
        CustomerEmail = "jane.doe@example.com",
        CustomerPhone = "+41791234567"
    };

    // ---- GetActiveRoutesAsync ----

    [Fact]
    public async Task GetActiveRoutesAsync_MapsRoutesToMinimalPublicShape()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { route });

        var result = await CreateService().GetActiveRoutesAsync();

        var dto = Assert.Single(result);
        Assert.Equal(route.Id, dto.Id);
        Assert.Equal("Basel", dto.DepartureLocation);
        Assert.Equal("Zurich", dto.Destination);
        Assert.Equal(180.00m, dto.Price);
        Assert.Equal("CHF", dto.Currency);
    }

    // ---- CreateBookingAsync — success path ----

    [Fact]
    public async Task CreateBookingAsync_WithValidData_Succeeds()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var result = await CreateService().CreateBookingAsync(ValidRequest(route.Id));

        Assert.True(result.Succeeded);
        Assert.Equal("LM-20260910-123456", result.Booking!.BookingReference);
        Assert.Equal("Pending", result.Booking!.Status);
        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _bookingRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_SnapshotsPriceAndCurrencyFromRoute_NotFromClient()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var result = await CreateService().CreateBookingAsync(ValidRequest(route.Id));

        Assert.Equal(route.Price, result.Booking!.Price);
        Assert.Equal(route.Currency, result.Booking!.Currency);
    }

    [Fact]
    public async Task CreateBookingAsync_LeavesDriverAndVehicleUnassigned()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        Booking? captured = null;
        _bookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        await CreateService().CreateBookingAsync(ValidRequest(route.Id));

        Assert.Null(captured!.DriverId);
        Assert.Null(captured.VehicleId);
    }

    // ---- CreateBookingAsync — route validation ----

    [Fact]
    public async Task CreateBookingAsync_UnknownRoute_ReturnsNotFound()
    {
        _routeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainRoute?)null);

        var result = await CreateService().CreateBookingAsync(ValidRequest(Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.NotFound, result.Error);
    }

    [Fact]
    public async Task CreateBookingAsync_InactiveRoute_IsRejected()
    {
        var route = ActiveRoute();
        route.Deactivate();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var result = await CreateService().CreateBookingAsync(ValidRequest(route.Id));

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.Validation, result.Error);
        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- CreateBookingAsync — passenger count ----

    [Fact]
    public async Task CreateBookingAsync_PassengerCountExceedsMaximum_IsRejected()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        var request = ValidRequest(route.Id);
        request.PassengerCount = 17;

        var result = await CreateService(new BookingSettings { MaximumPassengers = 16 }).CreateBookingAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateBookingAsync_PassengerCountAtMaximum_Succeeds()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        var request = ValidRequest(route.Id);
        request.PassengerCount = 16;

        var result = await CreateService(new BookingSettings { MaximumPassengers = 16 }).CreateBookingAsync(request);

        Assert.True(result.Succeeded);
    }

    // ---- CreateBookingAsync — past date / lead time (Europe/Zurich aware) ----

    [Fact]
    public async Task CreateBookingAsync_PickupTimeInThePast_IsRejected()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        // "Now" is 10:00 Zurich local (see FixedUtcNow); 09:00 is already in the past.
        var request = ValidRequest(route.Id, pickupTime: new TimeOnly(9, 0));

        var result = await CreateService().CreateBookingAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.Validation, result.Error);
        Assert.Contains("future", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBookingAsync_WithinMinimumLeadTime_IsRejected()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        // Now is 10:00 Zurich local; default lead time is 120 minutes, so 10:30 is
        // in the future but still inside the required lead-time window.
        var request = ValidRequest(route.Id, pickupTime: new TimeOnly(10, 30));

        var result = await CreateService().CreateBookingAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.Validation, result.Error);
        Assert.Contains("lead time", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBookingAsync_ExactlyAtMinimumLeadTime_Succeeds()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        // Now is 10:00 Zurich local; 12:00 is exactly 120 minutes out.
        var request = ValidRequest(route.Id, pickupTime: new TimeOnly(12, 0));

        var result = await CreateService().CreateBookingAsync(request);

        Assert.True(result.Succeeded);
    }

    // ---- CreateBookingAsync — customer data validation surfaces as Validation ----

    [Fact]
    public async Task CreateBookingAsync_InvalidCustomerEmail_IsRejected()
    {
        var route = ActiveRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        var request = ValidRequest(route.Id);
        request.CustomerEmail = "not-an-email";

        var result = await CreateService().CreateBookingAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(BookingError.Validation, result.Error);
    }
}
