using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Routes;
using Moq;
using DomainRoute = LimousineBooking.Domain.Entities.Route;

namespace LimousineBooking.Tests.Routes;

public class RouteServiceTests
{
    private readonly Mock<IRouteRepository> _routeRepository = new();

    private RouteService CreateService() => new(_routeRepository.Object);

    private static CreateRouteRequest ValidCreateRequest() => new()
    {
        DepartureLocation = "Basel",
        Destination = "Zurich",
        EstimatedDurationMinutes = 90,
        Price = 180.00m,
        Currency = "chf"
    };

    // ---- Create ----

    [Fact]
    public async Task Create_WithValidData_Succeeds()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync("Basel", "Zurich", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("Basel", result.Route!.DepartureLocation);
        Assert.Equal("CHF", result.Route!.Currency); // normalized to uppercase
        Assert.True(result.Route!.IsActive); // active by default
        _routeRepository.Verify(r => r.AddAsync(It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Once);
        _routeRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithMissingDeparture_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.DepartureLocation = "   ";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithMissingDestination_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Destination = "";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithInvalidDuration_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.EstimatedDurationMinutes = 0;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithNegativePrice_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Price = -1m;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithEmptyCurrency_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = ValidCreateRequest();
        request.Currency = "   ";

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_DuplicateActiveRoute_IsRejected()
    {
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync("Basel", "Zurich", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(ValidCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.Duplicate, result.Error);
        _routeRepository.Verify(r => r.AddAsync(It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_NormalizesWhitespaceAndCaseBeforeDuplicateCheck()
    {
        var request = ValidCreateRequest();
        request.DepartureLocation = "  Basel  ";
        request.Destination = "  Zurich ";

        _routeRepository.Setup(r => r.HasActiveDuplicateAsync("Basel", "Zurich", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(request);

        Assert.True(result.Succeeded);
        _routeRepository.Verify(r => r.HasActiveDuplicateAsync("Basel", "Zurich", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Update ----

    private static DomainRoute ExistingRoute() => new("Basel", "Zurich", 90, 180.00m, "CHF");

    [Fact]
    public async Task Update_ExistingRoute_Succeeds()
    {
        var route = ExistingRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync("Basel", "Bern", route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new UpdateRouteRequest
        {
            DepartureLocation = "Basel",
            Destination = "Bern",
            EstimatedDurationMinutes = 95,
            Price = 200.00m,
            Currency = "chf",
            IsActive = true
        };

        var result = await CreateService().UpdateAsync(route.Id, request);

        Assert.True(result.Succeeded);
        Assert.Equal("Bern", result.Route!.Destination);
        Assert.Equal(200.00m, result.Route!.Price);
        _routeRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistingRoute_ReturnsNotFound()
    {
        _routeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainRoute?)null);

        var result = await CreateService().UpdateAsync(Guid.NewGuid(), new UpdateRouteRequest
        {
            DepartureLocation = "Basel",
            Destination = "Bern",
            EstimatedDurationMinutes = 95,
            Price = 200.00m,
            Currency = "CHF",
            IsActive = true
        });

        Assert.False(result.Succeeded);
        Assert.Equal(RouteError.NotFound, result.Error);
    }

    // ---- Activation ----

    [Fact]
    public async Task SetActive_True_ActivatesRoute()
    {
        var route = ExistingRoute();
        route.Deactivate();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);
        _routeRepository.Setup(r => r.HasActiveDuplicateAsync("Basel", "Zurich", route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().SetActiveAsync(route.Id, true);

        Assert.True(result.Succeeded);
        Assert.True(result.Route!.IsActive);
    }

    [Fact]
    public async Task SetActive_False_DeactivatesRoute_WithoutDeletingIt()
    {
        var route = ExistingRoute();
        _routeRepository.Setup(r => r.GetByIdAsync(route.Id, It.IsAny<CancellationToken>())).ReturnsAsync(route);

        var result = await CreateService().SetActiveAsync(route.Id, false);

        Assert.True(result.Succeeded);
        Assert.False(result.Route!.IsActive);
        // The route itself is still returned/retrievable — deactivation is not deletion.
        Assert.Equal(route.Id, result.Route!.Id);
        _routeRepository.Verify(r => r.AddAsync(It.IsAny<DomainRoute>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
