using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainBooking = LimousineBooking.Domain.Entities.Booking;
using DomainDriver = LimousineBooking.Domain.Entities.Driver;
using DomainVehicle = LimousineBooking.Domain.Entities.Vehicle;

namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Orchestrates administrator booking management: search/detail, editing (with
/// price recalculation and assignment revalidation), manual assignment/reassignment,
/// cancellation, and triggering automatic reassignment. Delegates all eligibility
/// search/ranking to AutomaticAssignmentService (Prompt 9) — this class never
/// duplicates that logic, it only performs the narrower "is this ONE admin-chosen
/// driver+vehicle valid for this booking" check for manual assignment.
/// </summary>
public class AdminBookingService : IAdminBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAvailabilityEvaluationService _availabilityEvaluationService;
    private readonly IAutomaticAssignmentService _automaticAssignmentService;
    private readonly IAssignmentHistoryRepository _assignmentHistoryRepository;
    private readonly IRideStatusHistoryRepository _rideStatusHistoryRepository;
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BookingSettings _settings;
    private readonly ILogger<AdminBookingService> _logger;

    public AdminBookingService(
        IBookingRepository bookingRepository,
        IRouteRepository routeRepository,
        IDriverRepository driverRepository,
        IVehicleRepository vehicleRepository,
        IUserRepository userRepository,
        IAvailabilityEvaluationService availabilityEvaluationService,
        IAutomaticAssignmentService automaticAssignmentService,
        IAssignmentHistoryRepository assignmentHistoryRepository,
        IRideStatusHistoryRepository rideStatusHistoryRepository,
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ITransactionRunner transactionRunner,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IOptions<BookingSettings> settings,
        ILogger<AdminBookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _routeRepository = routeRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _userRepository = userRepository;
        _availabilityEvaluationService = availabilityEvaluationService;
        _automaticAssignmentService = automaticAssignmentService;
        _assignmentHistoryRepository = assignmentHistoryRepository;
        _rideStatusHistoryRepository = rideStatusHistoryRepository;
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _transactionRunner = transactionRunner;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PagedResult<AdminBookingListItemResponse>> SearchAsync(AdminBookingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _bookingRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<AdminBookingListItemResponse>
        {
            Items = items.Select(ToListItem).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminBookingDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return booking is null ? null : await ToDetailAsync(booking, cancellationToken);
    }

    public async Task<AdminBookingOperationResult> UpdateAsync(Guid id, UpdateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (booking is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Booking not found.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, $"{booking.Status} bookings cannot be edited.");

        var route = await _routeRepository.GetByIdAsync(request.RouteId, cancellationToken);
        if (route is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Route not found.");
        if (!route.IsActive)
            return AdminBookingOperationResult.Failure(AdminBookingError.Validation, "This route is not active.");

        if (request.PassengerCount > _settings.MaximumPassengers)
            return AdminBookingOperationResult.Failure(AdminBookingError.Validation, $"Passenger count must not exceed {_settings.MaximumPassengers}.");

        // The route ID, date, time, and passenger count together determine whether
        // the CURRENT assignment (if any) is still valid — see UnassignForRevalidation.
        var routeChanged = booking.RouteId != request.RouteId;
        var tripAffectingChange = routeChanged
            || booking.TravelDate != request.BookingDate
            || booking.PickupTime != request.PickupTime
            || booking.PassengerCount != request.PassengerCount;

        // Price is a snapshot (Prompt 8) — it never silently drifts with the route's
        // current price. It's only ever explicitly recalculated here, and only when
        // the route itself changed, exactly as the spec requires.
        var price = booking.Price;
        var currency = booking.Currency;
        if (routeChanged)
        {
            _logger.LogInformation(
                "Booking {BookingReference} price recalculated by administrator {AdminUserId}: {OldPrice} {OldCurrency} -> {NewPrice} {NewCurrency} (route {OldRouteId} -> {NewRouteId}).",
                booking.BookingReference, _currentUserService.UserId, price, currency, route.Price, route.Currency, booking.RouteId, route.Id);
            price = route.Price;
            currency = route.Currency;
        }

        try
        {
            booking.UpdateDetails(
                route.Id,
                request.BookingDate,
                request.PickupTime,
                request.PickupAddress.Trim(),
                request.PassengerCount,
                request.CustomerFirstName.Trim(),
                request.CustomerLastName.Trim(),
                request.CustomerEmail.Trim(),
                request.CustomerPhone.Trim(),
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                price,
                currency);
        }
        catch (ArgumentException ex)
        {
            return AdminBookingOperationResult.Failure(AdminBookingError.Validation, ex.Message);
        }

        if (tripAffectingChange)
        {
            // Don't duplicate eligibility logic: reset to unassigned and let
            // AutomaticAssignmentService decide from scratch. If the previous
            // driver is still the best eligible candidate, its deterministic
            // ranking will simply reselect them.
            booking.UnassignForRevalidation();
            await _bookingRepository.SaveChangesAsync(cancellationToken);
            await _automaticAssignmentService.AssignBookingAsync(booking.Id, cancellationToken);
        }
        else
        {
            await _bookingRepository.SaveChangesAsync(cancellationToken);
        }

        return AdminBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    public Task<AdminBookingOperationResult> AssignDriverAsync(Guid id, AssignDriverRequest request, CancellationToken cancellationToken = default) =>
        _transactionRunner.RunSerializableAsync(ct => AttemptManualAssignmentAsync(id, request, ct), cancellationToken);

    private async Task<AdminBookingOperationResult> AttemptManualAssignmentAsync(Guid id, AssignDriverRequest request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (booking is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Booking not found.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, $"{booking.Status} bookings cannot be assigned a driver.");
        if (booking.Route is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "The booking's route could not be loaded.");

        var driver = await _driverRepository.GetByIdAsync(request.DriverId, cancellationToken);
        if (driver is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Driver not found.");
        if (!driver.IsActive)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Driver is not active.");
        if (driver.User is null || !driver.User.IsActive)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Driver's user account is not active.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Vehicle not found.");
        if (!vehicle.IsActive)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Vehicle is not active.");

        // Driver <-> Vehicle compatibility: this domain models one "current vehicle"
        // per driver (Driver.CurrentVehicleId), not a many-to-many fleet relationship —
        // so compatibility means the requested vehicle IS the driver's current one.
        if (driver.CurrentVehicleId != vehicle.Id)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Driver does not have the selected vehicle.");

        if (vehicle.PassengerCapacity < booking.PassengerCount)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Vehicle capacity is insufficient.");

        // Section 15: v1 uses strict validation — an unavailable driver is rejected,
        // never silently overridden.
        if (!driver.IsAvailable)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "The selected driver is not available for this booking.");

        var tripStart = booking.TravelDate.ToDateTime(booking.PickupTime);
        var tripEnd = tripStart.AddMinutes(booking.Route.EstimatedDurationMinutes);

        var isScheduled = await _availabilityEvaluationService.IsDriverAvailableAsync(
            driver.Id, booking.TravelDate, booking.PickupTime, TimeOnly.FromDateTime(tripEnd), cancellationToken);
        if (!isScheduled)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "The selected driver does not have a matching availability schedule for this booking.");

        var conflictScan = await _bookingRepository.GetConflictScanAsync(
            booking.TravelDate, new[] { driver.Id }, new[] { vehicle.Id }, booking.Id, cancellationToken);
        var conflictReason = FindConflictReason(driver.Id, vehicle.Id, tripStart, tripEnd, conflictScan, _settings.DriverBufferMinutes);
        if (conflictReason is not null)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, conflictReason);

        // Captured before the assignment is overwritten — this is the only
        // moment the previous driver is still known, and reassignment-away
        // notifications need it.
        var previousDriverId = booking.DriverId;
        var previousDriver = booking.Driver;

        booking.ConfirmManualAssignment(driver.Id, vehicle.Id);

        await _assignmentHistoryRepository.AddAsync(
            new Domain.Entities.AssignmentHistory(booking.Id, driver.Id, vehicle.Id, AssignmentType.Manual, _currentUserService.UserId, _dateTimeProvider.UtcNow),
            cancellationToken);

        if (previousDriverId is null)
        {
            // First-time manual assignment (section 37: "Administrator manually
            // assigns driver" — distinct from reassignment's own notification set).
            await _notificationService.NotifyCustomerAssignedAsync(booking, booking.Route, driver, cancellationToken);
            await _notificationService.NotifyDriverAssignedAsync(booking, booking.Route, driver, cancellationToken);
        }
        else if (previousDriverId != driver.Id && previousDriver is not null)
        {
            await _notificationService.NotifyReassignedAsync(booking, booking.Route, previousDriver, driver, cancellationToken);
        }
        // Re-selecting the same driver that was already assigned is a no-op for
        // everyone involved — nothing to notify.

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingReference} manually assigned to driver {DriverId} / vehicle {VehicleId} by administrator {AdminUserId}.",
            booking.BookingReference, driver.Id, vehicle.Id, _currentUserService.UserId);

        return AdminBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    public async Task<AdminBookingOperationResult> CancelAsync(Guid id, CancelBookingRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (booking is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Booking not found.");
        if (booking.Status == BookingStatus.Cancelled)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "The booking is already cancelled.");
        if (booking.Status == BookingStatus.Completed)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "Completed bookings cannot be cancelled.");

        booking.Cancel(request.Reason, _currentUserService.UserId, _dateTimeProvider.UtcNow);

        if (booking.Route is not null)
            await _notificationService.NotifyCustomerCancelledAsync(booking, booking.Route, cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingReference} cancelled by administrator {AdminUserId}. Reason: {Reason}",
            booking.BookingReference, _currentUserService.UserId, request.Reason ?? "(none given)");

        return AdminBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    public async Task<AdminBookingOperationResult> AutoAssignAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (booking is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Booking not found.");
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, $"{booking.Status} bookings cannot be assigned a driver.");

        await _automaticAssignmentService.AssignBookingAsync(booking.Id, cancellationToken);

        return AdminBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    public async Task<AdminBookingOperationResult> ResendConfirmationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (booking is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.NotFound, "Booking not found.");
        if (booking.Route is null)
            return AdminBookingOperationResult.Failure(AdminBookingError.Conflict, "The booking's route could not be loaded.");

        await _notificationService.ResendConfirmationAsync(booking, booking.Route, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return AdminBookingOperationResult.Success(await ToDetailAsync(booking, cancellationToken));
    }

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(Common.SwissTimeZone.ConvertFromUtc(_dateTimeProvider.UtcNow));
        var startOfTodayUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(todayLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), Common.SwissTimeZone.Instance);

        var counts = await _bookingRepository.GetDashboardCountsAsync(todayLocal, cancellationToken);
        var upcoming = await _bookingRepository.GetUpcomingAsync(todayLocal, 10, cancellationToken);
        var notificationSummary = await _notificationRepository.GetSummaryAsync(startOfTodayUtc, cancellationToken);

        return new AdminDashboardResponse
        {
            TotalBookings = counts.TotalBookings,
            TodaysBookings = counts.TodaysBookings,
            PendingBookings = counts.PendingBookings,
            RequiresManualAssignmentCount = counts.RequiresManualAssignmentCount,
            ConfirmedBookings = counts.ConfirmedBookings,
            CancelledBookings = counts.CancelledBookings,
            UpcomingTripsCount = counts.UpcomingTripsCount,
            UpcomingBookings = upcoming.Select(ToUpcomingItem).ToList(),
            Notifications = notificationSummary
        };
    }

    /// <summary>
    /// Half-open interval overlap ([Start, End)) with the existing booking's interval
    /// padded by the buffer on both sides — identical rule to AutomaticAssignmentService,
    /// but returns which specific thing conflicted (driver or vehicle) so the admin gets
    /// a precise reason instead of a generic one.
    /// </summary>
    private static string? FindConflictReason(Guid driverId, Guid vehicleId, DateTime newStart, DateTime newEnd, IReadOnlyList<DomainBooking> conflictScan, int bufferMinutes)
    {
        foreach (var existing in conflictScan)
        {
            if (existing.Route is null)
                continue;

            var existingStart = existing.TravelDate.ToDateTime(existing.PickupTime);
            var existingEnd = existingStart.AddMinutes(existing.Route.EstimatedDurationMinutes);
            var bufferedStart = existingStart.AddMinutes(-bufferMinutes);
            var bufferedEnd = existingEnd.AddMinutes(bufferMinutes);

            if (bufferedStart >= newEnd || newStart >= bufferedEnd)
                continue;

            if (existing.DriverId == driverId)
                return "The selected driver already has another booking during this period.";
            if (existing.VehicleId == vehicleId)
                return "The selected vehicle is already assigned to another booking during this period.";
        }

        return null;
    }

    private static string FormatDriverName(DomainDriver? driver) =>
        driver?.User is null ? string.Empty : $"{driver.User.FirstName} {driver.User.LastName}";

    private static string FormatVehicleDescription(DomainVehicle? vehicle) =>
        vehicle is null ? string.Empty : $"{vehicle.Make} {vehicle.Model} - {vehicle.RegistrationNumber}";

    private static AdminBookingListItemResponse ToListItem(DomainBooking booking) => new()
    {
        Id = booking.Id,
        BookingReference = booking.BookingReference,
        CustomerFirstName = booking.CustomerFirstName,
        CustomerLastName = booking.CustomerLastName,
        Route = new BookingRouteSummary
        {
            DepartureLocation = booking.Route?.DepartureLocation ?? string.Empty,
            Destination = booking.Route?.Destination ?? string.Empty
        },
        BookingDate = booking.TravelDate,
        PickupTime = booking.PickupTime,
        PassengerCount = booking.PassengerCount,
        Price = booking.Price,
        Currency = booking.Currency,
        Status = booking.Status.ToString(),
        RideStatus = booking.RideStatus.ToString(),
        DriverName = booking.Driver is null ? null : FormatDriverName(booking.Driver),
        VehicleDescription = booking.Vehicle is null ? null : FormatVehicleDescription(booking.Vehicle),
        Assignment = booking.AssignmentType?.ToString() ?? "Unassigned"
    };

    private static UpcomingBookingItem ToUpcomingItem(DomainBooking booking) => new()
    {
        Id = booking.Id,
        BookingReference = booking.BookingReference,
        BookingDate = booking.TravelDate,
        PickupTime = booking.PickupTime,
        Route = new BookingRouteSummary
        {
            DepartureLocation = booking.Route?.DepartureLocation ?? string.Empty,
            Destination = booking.Route?.Destination ?? string.Empty
        },
        CustomerFirstName = booking.CustomerFirstName,
        CustomerLastName = booking.CustomerLastName,
        DriverName = booking.Driver is null ? null : FormatDriverName(booking.Driver),
        VehicleDescription = booking.Vehicle is null ? null : FormatVehicleDescription(booking.Vehicle),
        Status = booking.Status.ToString()
    };

    private async Task<AdminBookingDetailResponse> ToDetailAsync(DomainBooking booking, CancellationToken cancellationToken)
    {
        string? cancelledByEmail = null;
        if (booking.CancelledByUserId.HasValue)
        {
            var cancelledByUser = await _userRepository.GetByIdAsync(booking.CancelledByUserId.Value, cancellationToken);
            cancelledByEmail = cancelledByUser?.Email;
        }

        var rideStatusEntries = await _rideStatusHistoryRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        var rideStatusHistoryItems = rideStatusEntries.Select(r => new RideStatusHistoryEntry
        {
            PreviousStatus = r.PreviousStatus.ToString(),
            NewStatus = r.NewStatus.ToString(),
            ChangedAt = r.ChangedAt
        }).ToList();

        var historyEntries = await _assignmentHistoryRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        var historyItems = new List<AssignmentHistoryItem>();
        foreach (var entry in historyEntries)
        {
            var entryDriver = await _driverRepository.GetByIdAsync(entry.DriverId, cancellationToken);
            var entryVehicle = await _vehicleRepository.GetByIdAsync(entry.VehicleId, cancellationToken);

            string? assignedByEmail = null;
            if (entry.AssignedByUserId.HasValue)
            {
                var assignedByUser = await _userRepository.GetByIdAsync(entry.AssignedByUserId.Value, cancellationToken);
                assignedByEmail = assignedByUser?.Email;
            }

            historyItems.Add(new AssignmentHistoryItem
            {
                DriverName = FormatDriverName(entryDriver),
                VehicleDescription = FormatVehicleDescription(entryVehicle),
                AssignmentType = entry.AssignmentType.ToString(),
                AssignedByEmail = assignedByEmail,
                AssignedAt = entry.AssignedAt
            });
        }

        var route = booking.Route;
        var estimatedEndTime = route is null
            ? booking.PickupTime
            : TimeOnly.FromDateTime(booking.TravelDate.ToDateTime(booking.PickupTime).AddMinutes(route.EstimatedDurationMinutes));

        return new AdminBookingDetailResponse
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            CustomerFirstName = booking.CustomerFirstName,
            CustomerLastName = booking.CustomerLastName,
            CustomerEmail = booking.CustomerEmail,
            CustomerPhone = booking.CustomerPhone,
            RouteId = booking.RouteId,
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
            Price = booking.Price,
            Currency = booking.Currency,
            Status = booking.Status.ToString(),
            RideStatus = booking.RideStatus.ToString(),
            RideStatusHistory = rideStatusHistoryItems,
            DriverId = booking.DriverId,
            DriverName = booking.Driver is null ? null : FormatDriverName(booking.Driver),
            VehicleId = booking.VehicleId,
            VehicleDescription = booking.Vehicle is null ? null : FormatVehicleDescription(booking.Vehicle),
            AssignmentType = booking.AssignmentType?.ToString(),
            RequiresManualAssignment = booking.RequiresManualAssignment,
            ManualAssignmentReason = booking.ManualAssignmentReason,
            CancellationReason = booking.CancellationReason,
            CancelledAt = booking.CancelledAt,
            CancelledByEmail = cancelledByEmail,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            AssignmentHistory = historyItems
        };
    }
}
