using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Finds an eligible driver + vehicle for a freshly created booking and assigns
/// them automatically. Isolated from PublicBookingService/controllers so the
/// eligibility/ranking rules live in exactly one place and can be reused (e.g. a
/// future admin "retry assignment" action) without duplicating them.
///
/// The whole candidate search + save runs inside one Serializable transaction
/// (via ITransactionRunner) so two concurrent bookings can never both be
/// assigned the same driver for overlapping trips — see TransactionRunner.
/// </summary>
public class AutomaticAssignmentService : IAutomaticAssignmentService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IAvailabilityEvaluationService _availabilityEvaluationService;
    private readonly IAssignmentHistoryRepository _assignmentHistoryRepository;
    private readonly INotificationService _notificationService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BookingSettings _settings;
    private readonly ILogger<AutomaticAssignmentService> _logger;

    public AutomaticAssignmentService(
        IBookingRepository bookingRepository,
        IDriverRepository driverRepository,
        IAvailabilityEvaluationService availabilityEvaluationService,
        IAssignmentHistoryRepository assignmentHistoryRepository,
        INotificationService notificationService,
        ITransactionRunner transactionRunner,
        IDateTimeProvider dateTimeProvider,
        IOptions<BookingSettings> settings,
        ILogger<AutomaticAssignmentService> logger)
    {
        _bookingRepository = bookingRepository;
        _driverRepository = driverRepository;
        _availabilityEvaluationService = availabilityEvaluationService;
        _assignmentHistoryRepository = assignmentHistoryRepository;
        _notificationService = notificationService;
        _transactionRunner = transactionRunner;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task AssignBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _transactionRunner.RunSerializableAsync(ct => AttemptAssignmentAsync(bookingId, ct), cancellationToken);

    private async Task<bool> AttemptAssignmentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, cancellationToken);
        if (booking?.Route is null)
        {
            _logger.LogWarning("Automatic assignment skipped — booking {BookingId} or its route could not be loaded.", bookingId);
            return false;
        }

        var tripStart = booking.TravelDate.ToDateTime(booking.PickupTime);
        var tripEnd = tripStart.AddMinutes(booking.Route.EstimatedDurationMinutes);

        // Step 1: DB-filtered candidates — active, active user, currently
        // available, has an active vehicle with enough seats (section 5.1-4, 6, 7, 12, 13, 33, 34, 35).
        var candidates = await _driverRepository.GetAssignmentCandidatesAsync(booking.PassengerCount, cancellationToken);
        if (candidates.Count == 0)
            return await FailAsync(booking, bookingId, 0, "No active driver with a suitable, sufficiently large vehicle is currently available.", cancellationToken);

        // Step 2: scheduled availability, reusing the Prompt 7 evaluation service
        // rather than duplicating its schedule logic here (section 5.5, 8, 30, 36).
        var scheduled = new List<Driver>();
        foreach (var candidate in candidates)
        {
            var isScheduled = await _availabilityEvaluationService.IsDriverAvailableAsync(
                candidate.Id, booking.TravelDate, booking.PickupTime, TimeOnly.FromDateTime(tripEnd), cancellationToken);
            if (isScheduled)
                scheduled.Add(candidate);
        }

        if (scheduled.Count == 0)
            return await FailAsync(booking, bookingId, candidates.Count, "No candidate driver has a matching availability schedule for this trip.", cancellationToken);

        // Step 3: booking/vehicle conflicts, with buffer (section 5.6, 9-14, 37-39).
        var driverIds = scheduled.Select(d => d.Id).ToList();
        var vehicleIds = scheduled.Select(d => d.CurrentVehicleId!.Value).ToList();
        var conflictScan = await _bookingRepository.GetConflictScanAsync(booking.TravelDate, driverIds, vehicleIds, bookingId, cancellationToken);

        var conflictFree = scheduled
            .Where(d => !HasConflict(d, tripStart, tripEnd, conflictScan, _settings.DriverBufferMinutes))
            .ToList();

        if (conflictFree.Count == 0)
            return await FailAsync(booking, bookingId, scheduled.Count, "All otherwise-eligible drivers have a conflicting booking.", cancellationToken);

        // Step 4: rank and select (section 15-17) — smallest sufficient vehicle,
        // then least busy driver, then driver ID as a stable, deterministic tie-breaker.
        var workload = await _bookingRepository.GetUpcomingBookingCountsAsync(
            conflictFree.Select(d => d.Id).ToList(), booking.TravelDate, cancellationToken);

        var selected = conflictFree
            .OrderBy(d => d.CurrentVehicle!.PassengerCapacity)
            .ThenBy(d => workload.GetValueOrDefault(d.Id, 0))
            .ThenBy(d => d.Id)
            .First();

        booking.ConfirmAutomaticAssignment(selected.Id, selected.CurrentVehicleId!.Value);

        await _assignmentHistoryRepository.AddAsync(
            new AssignmentHistory(booking.Id, selected.Id, selected.CurrentVehicleId!.Value, AssignmentType.Automatic, assignedByUserId: null, _dateTimeProvider.UtcNow),
            cancellationToken);

        // Enqueued before SaveChangesAsync so the notification rows ride along
        // in the same transaction as the assignment itself (transactional outbox
        // — see Notification's summary).
        await _notificationService.NotifyCustomerBookingConfirmedAsync(booking, booking.Route, cancellationToken);
        await _notificationService.NotifyDriverAssignedAsync(booking, booking.Route, selected, cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingReference} automatically assigned to driver {DriverId} / vehicle {VehicleId} ({EligibleCount} eligible candidate(s) considered).",
            booking.BookingReference, selected.Id, selected.CurrentVehicleId, conflictFree.Count);

        return true;
    }

    private async Task<bool> FailAsync(Booking booking, Guid bookingId, int candidateCount, string reason, CancellationToken cancellationToken)
    {
        booking.MarkRequiresManualAssignment(reason);

        if (booking.Route is not null)
        {
            await _notificationService.NotifyCustomerBookingPendingAsync(booking, booking.Route, cancellationToken);
            await _notificationService.NotifyAdminManualAssignmentRequiredAsync(booking, booking.Route, reason, cancellationToken);
        }

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} could not be automatically assigned ({CandidateCount} candidate(s) considered). Reason: {Reason}",
            bookingId, candidateCount, reason);

        return false;
    }

    /// <summary>
    /// Half-open interval overlap ([Start, End)), with the *existing* booking's
    /// interval padded by the configured buffer on both sides — this protects the
    /// required gap whether the new booking would come immediately before or
    /// immediately after the existing one, using a single symmetric check.
    /// </summary>
    private static bool HasConflict(Driver driver, DateTime newStart, DateTime newEnd, IReadOnlyList<Booking> conflictScan, int bufferMinutes)
    {
        foreach (var existing in conflictScan)
        {
            var matchesDriver = existing.DriverId == driver.Id;
            var matchesVehicle = existing.VehicleId == driver.CurrentVehicleId;
            if (!matchesDriver && !matchesVehicle)
                continue;
            if (existing.Route is null)
                continue;

            var existingStart = existing.TravelDate.ToDateTime(existing.PickupTime);
            var existingEnd = existingStart.AddMinutes(existing.Route.EstimatedDurationMinutes);
            var bufferedStart = existingStart.AddMinutes(-bufferMinutes);
            var bufferedEnd = existingEnd.AddMinutes(bufferMinutes);

            if (bufferedStart < newEnd && newStart < bufferedEnd)
                return true;
        }

        return false;
    }
}
