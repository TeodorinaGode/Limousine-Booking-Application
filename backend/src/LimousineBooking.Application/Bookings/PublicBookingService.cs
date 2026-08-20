using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.Options;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;

namespace LimousineBooking.Application.Bookings;

public class PublicBookingService : IPublicBookingService
{
    private readonly IRouteRepository _routeRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingReferenceGenerator _referenceGenerator;
    private readonly IAutomaticAssignmentService _automaticAssignmentService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BookingSettings _settings;

    public PublicBookingService(
        IRouteRepository routeRepository,
        IBookingRepository bookingRepository,
        IBookingReferenceGenerator referenceGenerator,
        IAutomaticAssignmentService automaticAssignmentService,
        IDateTimeProvider dateTimeProvider,
        IOptions<BookingSettings> settings)
    {
        _routeRepository = routeRepository;
        _bookingRepository = bookingRepository;
        _referenceGenerator = referenceGenerator;
        _automaticAssignmentService = automaticAssignmentService;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<PublicRouteResponse>> GetActiveRoutesAsync(CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetActiveAsync(cancellationToken);

        return routes.Select(r => new PublicRouteResponse
        {
            Id = r.Id,
            DepartureLocation = r.DepartureLocation,
            Destination = r.Destination,
            EstimatedDurationMinutes = r.EstimatedDurationMinutes,
            Price = r.Price,
            Currency = r.Currency
        }).ToList();
    }

    public async Task<BookingOperationResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(request.RouteId, cancellationToken);
        if (route is null)
            return BookingOperationResult.Failure(BookingError.NotFound, "Route not found.");
        if (!route.IsActive)
            return BookingOperationResult.Failure(BookingError.Validation, "This route is no longer available for booking.");

        if (request.PassengerCount > _settings.MaximumPassengers)
            return BookingOperationResult.Failure(BookingError.Validation, $"Passenger count must not exceed {_settings.MaximumPassengers}.");

        var leadTimeError = ValidateLeadTime(request.BookingDate, request.PickupTime);
        if (leadTimeError is not null)
            return BookingOperationResult.Failure(BookingError.Validation, leadTimeError);

        var bookingReference = await _referenceGenerator.GenerateAsync(request.BookingDate, cancellationToken);

        DomainBooking booking;
        try
        {
            booking = new DomainBooking(
                bookingReference,
                request.CustomerFirstName.Trim(),
                request.CustomerLastName.Trim(),
                request.CustomerEmail.Trim(),
                request.CustomerPhone.Trim(),
                route.Id,
                request.BookingDate,
                request.PickupTime,
                request.PickupAddress.Trim(),
                request.PassengerCount,
                route.Price,
                route.Currency,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim());
        }
        catch (ArgumentException ex)
        {
            return BookingOperationResult.Failure(BookingError.Validation, ex.Message);
        }

        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        // Automatic assignment never fails the booking itself — a customer whose
        // trip has no eligible driver right now still gets a successful response;
        // the booking just stays Pending with RequiresManualAssignment = true for
        // an administrator to pick up later. See AutomaticAssignmentService.
        await _automaticAssignmentService.AssignBookingAsync(booking.Id, cancellationToken);

        return BookingOperationResult.Success(ToResponse(booking, route));
    }

    /// <summary>
    /// Requested pickup date/time is interpreted as Europe/Zurich local time (the
    /// only timezone this app operates in) and must be far enough in the future.
    /// A too-close or past date/time both fail this check — the past case is just
    /// the lead time requirement taken to its extreme, not a separate rule.
    /// </summary>
    private string? ValidateLeadTime(DateOnly bookingDate, TimeOnly pickupTime)
    {
        var requestedPickupLocal = bookingDate.ToDateTime(pickupTime);
        var nowLocal = SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow);

        if (requestedPickupLocal <= nowLocal)
            return "Booking date and time must be in the future.";

        var earliestAllowed = nowLocal.AddMinutes(_settings.MinimumLeadTimeMinutes);
        if (requestedPickupLocal < earliestAllowed)
            return $"Bookings require at least {_settings.MinimumLeadTimeMinutes} minutes of lead time.";

        return null;
    }

    private static BookingResponse ToResponse(DomainBooking booking, Domain.Entities.Route route) => new()
    {
        Id = booking.Id,
        BookingReference = booking.BookingReference,
        AccessToken = booking.PublicAccessToken,
        Status = booking.Status.ToString(),
        Route = new BookingRouteSummary
        {
            DepartureLocation = route.DepartureLocation,
            Destination = route.Destination
        },
        BookingDate = booking.TravelDate,
        PickupTime = booking.PickupTime,
        PickupAddress = booking.PickupAddress,
        PassengerCount = booking.PassengerCount,
        Notes = booking.Notes,
        Price = booking.Price,
        Currency = booking.Currency
    };
}
