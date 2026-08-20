using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainRideStatusHistory = LimousineBooking.Domain.Entities.RideStatusHistory;

namespace LimousineBooking.Application.Drivers;

/// <summary>
/// Orchestrates the authenticated driver's own dashboard, schedule, trip detail,
/// and ride-status transitions. Ride-status transitions run inside the same
/// Serializable-transaction protection as admin assignment (Prompt 9/10) — a
/// double-tap on "Start Ride" from two devices must not both succeed. Every
/// pre-check (driver active, booking ownership, current ride state) happens
/// here so the caller gets a specific 404/409 reason; the domain methods on
/// Booking itself only re-check as defense-in-depth.
/// </summary>
public class DriverBookingService : IDriverBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IRideStatusHistoryRepository _rideStatusHistoryRepository;
    private readonly INotificationService _notificationService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DriverBookingService> _logger;

    public DriverBookingService(
        IBookingRepository bookingRepository,
        IDriverRepository driverRepository,
        IRideStatusHistoryRepository rideStatusHistoryRepository,
        INotificationService notificationService,
        ITransactionRunner transactionRunner,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<DriverBookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _driverRepository = driverRepository;
        _rideStatusHistoryRepository = rideStatusHistoryRepository;
        _notificationService = notificationService;
        _transactionRunner = transactionRunner;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<DriverDashboardResponse> GetDashboardAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(Common.SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow));

        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        var todaysBookings = await _bookingRepository.GetByDriverAndDateAsync(driverId, todayLocal, cancellationToken);
        var upcomingCount = await _bookingRepository.CountUpcomingByDriverAsync(driverId, todayLocal, cancellationToken);

        var nextTrip = todaysBookings
            .Where(b => b.RideStatus != RideStatus.Completed)
            .OrderBy(b => b.PickupTime)
            .FirstOrDefault();

        return new DriverDashboardResponse
        {
            Today = todayLocal,
            IsAvailable = driver?.IsAvailable ?? false,
            TodaysTripCount = todaysBookings.Count,
            CompletedTodayCount = todaysBookings.Count(b => b.RideStatus == RideStatus.Completed),
            UpcomingTripCount = upcomingCount,
            TodaysTrips = todaysBookings.Select(ToListItem).ToList(),
            NextTrip = nextTrip is null ? null : ToListItem(nextTrip)
        };
    }

    public async Task<PagedResult<DriverBookingListItemResponse>> SearchAsync(Guid driverId, DriverBookingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _bookingRepository.SearchByDriverAsync(driverId, query, cancellationToken);

        return new PagedResult<DriverBookingListItemResponse>
        {
            Items = items.Select(ToListItem).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DriverBookingDetailResponse?> GetByIdAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByDriverAndIdAsync(driverId, bookingId, cancellationToken);
        return booking is null ? null : await ToDetailAsync(booking, cancellationToken);
    }

    public Task<DriverBookingOperationResult> StartRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default) =>
        _transactionRunner.RunSerializableAsync(ct => AttemptStartRideAsync(driverId, bookingId, ct), cancellationToken);

    public Task<DriverBookingOperationResult> MarkPassengerPickedUpAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default) =>
        _transactionRunner.RunSerializableAsync(ct => AttemptMarkPassengerPickedUpAsync(driverId, bookingId, ct), cancellationToken);

    public Task<DriverBookingOperationResult> CompleteRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default) =>
        _transactionRunner.RunSerializableAsync(ct => AttemptCompleteRideAsync(driverId, bookingId, ct), cancellationToken);

    private async Task<DriverBookingOperationResult> AttemptStartRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, error) = await ValidateDriverAndBookingAsync(driverId, bookingId, cancellationToken);
        if (error is not null)
            return error;

        var message = booking!.RideStatus switch
        {
            RideStatus.Upcoming => null,
            RideStatus.Completed => "This ride has already been completed.",
            RideStatus.Cancelled => "This booking has been cancelled.",
            _ => "The ride has already started."
        };
        if (message is not null)
            return DriverBookingOperationResult.Failure(DriverBookingError.Conflict, message);

        return await ApplyTransitionAsync(booking, booking.StartRide, afterTransition: null, cancellationToken);
    }

    private async Task<DriverBookingOperationResult> AttemptMarkPassengerPickedUpAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, error) = await ValidateDriverAndBookingAsync(driverId, bookingId, cancellationToken);
        if (error is not null)
            return error;

        var message = booking!.RideStatus switch
        {
            RideStatus.OnTheWay => null,
            RideStatus.Upcoming => "The ride must be started before the passenger can be picked up.",
            RideStatus.Completed => "This ride has already been completed.",
            RideStatus.Cancelled => "This booking has been cancelled.",
            _ => "The passenger has already been picked up."
        };
        if (message is not null)
            return DriverBookingOperationResult.Failure(DriverBookingError.Conflict, message);

        return await ApplyTransitionAsync(booking, booking.MarkPassengerPickedUp, afterTransition: null, cancellationToken);
    }

    private async Task<DriverBookingOperationResult> AttemptCompleteRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (booking, error) = await ValidateDriverAndBookingAsync(driverId, bookingId, cancellationToken);
        if (error is not null)
            return error;

        var message = booking!.RideStatus switch
        {
            RideStatus.PassengerPickedUp => null,
            RideStatus.Completed => "This ride has already been completed.",
            RideStatus.Cancelled => "This booking has been cancelled.",
            _ => "The passenger must be picked up before the ride can be completed."
        };
        if (message is not null)
            return DriverBookingOperationResult.Failure(DriverBookingError.Conflict, message);

        return await ApplyTransitionAsync(
            booking,
            booking.CompleteRide,
            afterTransition: () => _notificationService.NotifyCustomerCompletedAsync(booking, booking.Route!, cancellationToken),
            cancellationToken);
    }

    /// <summary>Driver-active, booking-ownership, booking-lifecycle, and route-loaded checks shared by every transition.</summary>
    private async Task<(DomainBooking? Booking, DriverBookingOperationResult? Error)> ValidateDriverAndBookingAsync(
        Guid driverId, Guid bookingId, CancellationToken cancellationToken)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver is null || !driver.IsActive || driver.User is null || !driver.User.IsActive)
            return (null, DriverBookingOperationResult.Failure(DriverBookingError.Conflict, "Driver is not active."));

        var booking = await _bookingRepository.GetByDriverAndIdAsync(driverId, bookingId, cancellationToken);
        if (booking is null)
            return (null, DriverBookingOperationResult.Failure(DriverBookingError.NotFound, "Booking not found."));
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return (null, DriverBookingOperationResult.Failure(DriverBookingError.Conflict, $"{booking.Status} bookings cannot be updated."));
        if (booking.Route is null)
            return (null, DriverBookingOperationResult.Failure(DriverBookingError.Conflict, "The booking's route could not be loaded."));

        return (booking, null);
    }

    private async Task<DriverBookingOperationResult> ApplyTransitionAsync(
        DomainBooking booking, Action transition, Func<Task>? afterTransition, CancellationToken cancellationToken)
    {
        var previousStatus = booking.RideStatus;

        try
        {
            transition();
        }
        catch (InvalidOperationException ex)
        {
            // Defense-in-depth only — the switch-based pre-checks above already
            // produce the user-facing message for every reachable case.
            return DriverBookingOperationResult.Failure(DriverBookingError.Conflict, ex.Message);
        }

        await _rideStatusHistoryRepository.AddAsync(
            new DomainRideStatusHistory(booking.Id, previousStatus, booking.RideStatus, _currentUserService.UserId!.Value, _dateTimeProvider.UtcNow),
            cancellationToken);

        if (afterTransition is not null)
            await afterTransition();

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingReference} ride status changed {PreviousStatus} -> {NewStatus} by driver user {UserId}.",
            booking.BookingReference, previousStatus, booking.RideStatus, _currentUserService.UserId);

        return DriverBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    private static DriverBookingListItemResponse ToListItem(DomainBooking booking) => new()
    {
        Id = booking.Id,
        BookingReference = booking.BookingReference,
        Route = new BookingRouteSummary
        {
            DepartureLocation = booking.Route?.DepartureLocation ?? string.Empty,
            Destination = booking.Route?.Destination ?? string.Empty
        },
        BookingDate = booking.TravelDate,
        PickupTime = booking.PickupTime,
        PickupAddress = booking.PickupAddress,
        PassengerCount = booking.PassengerCount,
        CustomerFirstName = booking.CustomerFirstName,
        CustomerLastName = booking.CustomerLastName,
        Status = booking.Status.ToString(),
        RideStatus = booking.RideStatus.ToString()
    };

    private async Task<DriverBookingDetailResponse> ToDetailAsync(DomainBooking booking, CancellationToken cancellationToken)
    {
        var historyEntries = await _rideStatusHistoryRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        var route = booking.Route;
        var estimatedEndTime = route is null
            ? booking.PickupTime
            : TimeOnly.FromDateTime(booking.TravelDate.ToDateTime(booking.PickupTime).AddMinutes(route.EstimatedDurationMinutes));

        return new DriverBookingDetailResponse
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            CustomerFirstName = booking.CustomerFirstName,
            CustomerLastName = booking.CustomerLastName,
            CustomerPhone = booking.CustomerPhone,
            Route = new BookingRouteSummary
            {
                DepartureLocation = route?.DepartureLocation ?? string.Empty,
                Destination = route?.Destination ?? string.Empty
            },
            BookingDate = booking.TravelDate,
            PickupTime = booking.PickupTime,
            EstimatedDurationMinutes = route?.EstimatedDurationMinutes ?? 0,
            EstimatedEndTime = estimatedEndTime,
            PickupAddress = booking.PickupAddress,
            PassengerCount = booking.PassengerCount,
            Notes = booking.Notes,
            Status = booking.Status.ToString(),
            RideStatus = booking.RideStatus.ToString(),
            RideStatusHistory = historyEntries.Select(h => new RideStatusHistoryItem
            {
                PreviousStatus = h.PreviousStatus.ToString(),
                NewStatus = h.NewStatus.ToString(),
                ChangedAt = h.ChangedAt
            }).ToList()
        };
    }
}
