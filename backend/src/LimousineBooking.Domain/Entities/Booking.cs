using System.Security.Cryptography;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;
using BookingAssignmentType = LimousineBooking.Domain.Enums.AssignmentType;

namespace LimousineBooking.Domain.Entities;

public class Booking : AuditableEntity
{
    public string BookingReference { get; private set; } = string.Empty;

    /// <summary>
    /// A cryptographically random per-booking secret (never derived from
    /// BookingReference, which is only a 6-digit random suffix — too small a
    /// space to treat as a security boundary). Required, alongside
    /// BookingReference, by every public payment endpoint so payment status
    /// cannot be read by guessing a reference (see IPublicPaymentService).
    /// </summary>
    public string PublicAccessToken { get; private set; } = string.Empty;

    /// <summary>
    /// The language the customer was using when they created this booking (en/de/fr,
    /// normalized via <see cref="Common.SupportedLanguages.Normalize"/> — never an
    /// unsupported/arbitrary code). Captured once, at creation, rather than resolved from
    /// the customer's current browser language at send time — the customer may have booked
    /// in German and be reading their confirmation email later on a different device/browser,
    /// so this is the only reliable source for which language their emails should use.
    /// </summary>
    public string LanguageCode { get; private set; } = Common.SupportedLanguages.Default;

    public string CustomerFirstName { get; private set; } = string.Empty;
    public string CustomerLastName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;

    public Guid RouteId { get; private set; }
    public DateOnly TravelDate { get; private set; }
    public TimeOnly PickupTime { get; private set; }
    public string PickupAddress { get; private set; } = string.Empty;
    public int PassengerCount { get; private set; }
    public string? Notes { get; private set; }

    public Guid? DriverId { get; private set; }
    public Guid? VehicleId { get; private set; }

    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// True when automatic assignment could not find an eligible driver+vehicle and
    /// an administrator must assign one manually. Deliberately separate from
    /// <see cref="Status"/> — a booking can be Pending with this false (brand new,
    /// assignment not attempted/finished yet) or Pending with this true (assignment
    /// was attempted and failed).
    /// </summary>
    public bool RequiresManualAssignment { get; private set; }

    /// <summary>Internal, admin-facing explanation of why automatic assignment failed. Never shown to the customer.</summary>
    public string? ManualAssignmentReason { get; private set; }

    /// <summary>Null until a driver/vehicle is assigned; then records whether it happened automatically or via admin action.</summary>
    public AssignmentType? AssignmentType { get; private set; }

    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    /// <summary>Trip progress — independent of <see cref="Status"/>. See RideStatus's own summary.</summary>
    public RideStatus RideStatus { get; private set; } = RideStatus.Upcoming;

    public Route? Route { get; private set; }
    public Driver? Driver { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public ICollection<BookingStatusHistory> StatusHistory { get; private set; } = new List<BookingStatusHistory>();
    public ICollection<RideStatusHistory> RideStatusHistory { get; private set; } = new List<RideStatusHistory>();
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    /// <summary>Every payment attempt for this booking, oldest first — failed/expired attempts are never deleted (audit trail).</summary>
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    private Booking()
    {
    }

    public Booking(
        string bookingReference,
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string customerPhone,
        Guid routeId,
        DateOnly travelDate,
        TimeOnly pickupTime,
        string pickupAddress,
        int passengerCount,
        decimal price,
        string currency,
        string? notes = null,
        string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(bookingReference))
            throw new ArgumentException("Booking reference is required.", nameof(bookingReference));

        ValidateEditableFields(customerFirstName, customerLastName, customerEmail, customerPhone, routeId, pickupAddress, passengerCount, price, currency);

        BookingReference = bookingReference;
        PublicAccessToken = GeneratePublicAccessToken();
        LanguageCode = Common.SupportedLanguages.Normalize(languageCode);
        CustomerFirstName = customerFirstName;
        CustomerLastName = customerLastName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        RouteId = routeId;
        TravelDate = travelDate;
        PickupTime = pickupTime;
        PickupAddress = pickupAddress;
        PassengerCount = passengerCount;
        Price = price;
        Currency = currency;
        Notes = notes;
        Status = BookingStatus.Pending;
    }

    /// <summary>256 bits of randomness, base64url-encoded (~43 chars, URL-safe with no padding) — see PublicAccessToken's summary.</summary>
    private static string GeneratePublicAccessToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// Administrator edit of the fields a customer originally supplied, plus the
    /// price/currency the caller has already decided on (see AdminBookingService —
    /// unchanged unless the route itself changed, since price is a snapshot, not
    /// something that silently drifts with the route's current price).
    /// </summary>
    public void UpdateDetails(
        Guid routeId,
        DateOnly travelDate,
        TimeOnly pickupTime,
        string pickupAddress,
        int passengerCount,
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string customerPhone,
        string? notes,
        decimal price,
        string currency)
    {
        ValidateEditableFields(customerFirstName, customerLastName, customerEmail, customerPhone, routeId, pickupAddress, passengerCount, price, currency);

        RouteId = routeId;
        TravelDate = travelDate;
        PickupTime = pickupTime;
        PickupAddress = pickupAddress;
        PassengerCount = passengerCount;
        CustomerFirstName = customerFirstName;
        CustomerLastName = customerLastName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        Notes = notes;
        Price = price;
        Currency = currency;
    }

    private static void ValidateEditableFields(
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string customerPhone,
        Guid routeId,
        string pickupAddress,
        int passengerCount,
        decimal price,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(customerFirstName))
            throw new ArgumentException("Customer first name is required.", nameof(customerFirstName));
        if (string.IsNullOrWhiteSpace(customerLastName))
            throw new ArgumentException("Customer last name is required.", nameof(customerLastName));
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        if (!EmailFormat.IsValid(customerEmail))
            throw new ArgumentException("Customer email format is invalid.", nameof(customerEmail));
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ArgumentException("Customer phone is required.", nameof(customerPhone));
        if (!PhoneFormat.IsValid(customerPhone))
            throw new ArgumentException("Customer phone format is invalid.", nameof(customerPhone));
        if (routeId == Guid.Empty)
            throw new ArgumentException("RouteId is required.", nameof(routeId));
        if (string.IsNullOrWhiteSpace(pickupAddress))
            throw new ArgumentException("Pickup address is required.", nameof(pickupAddress));
        if (passengerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(passengerCount), "Passenger count must be greater than zero.");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must not be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));
    }

    public void AssignDriver(Guid driverId)
    {
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        DriverId = driverId;
    }

    public void AssignVehicle(Guid vehicleId)
    {
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        VehicleId = vehicleId;
    }

    public void ChangeStatus(BookingStatus newStatus) => Status = newStatus;

    /// <summary>
    /// Called by AutomaticAssignmentService when an eligible driver+vehicle pair is
    /// found. Moves the booking straight to Confirmed — there is no intermediate
    /// "assigned but not yet confirmed" state for automatic assignment in v1.
    /// </summary>
    public void ConfirmAutomaticAssignment(Guid driverId, Guid vehicleId)
    {
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        DriverId = driverId;
        VehicleId = vehicleId;
        AssignmentType = BookingAssignmentType.Automatic;
        RequiresManualAssignment = false;
        ManualAssignmentReason = null;
        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Called when automatic assignment cannot find an eligible driver+vehicle.
    /// The booking stays Pending with no driver/vehicle — RequiresManualAssignment
    /// is a separate flag precisely so an administrator can find these later.
    /// </summary>
    public void MarkRequiresManualAssignment(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required.", nameof(reason));

        RequiresManualAssignment = true;
        ManualAssignmentReason = reason;
    }

    /// <summary>Called by an administrator's explicit assignment/reassignment (Prompt 10) — the vehicle-compatibility and eligibility checks all happen before this is called.</summary>
    public void ConfirmManualAssignment(Guid driverId, Guid vehicleId)
    {
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        DriverId = driverId;
        VehicleId = vehicleId;
        AssignmentType = BookingAssignmentType.Manual;
        RequiresManualAssignment = false;
        ManualAssignmentReason = null;
        Status = BookingStatus.Confirmed;
    }

    /// <summary>
    /// Resets a booking to its pre-assignment state so AutomaticAssignmentService can
    /// re-run from a clean slate — used when an administrator edits a trip-affecting
    /// field (route/date/time/passenger count) on an already-assigned booking. Status
    /// must move back to Pending here (not left as Confirmed), otherwise a booking
    /// with no driver could incorrectly still read as Confirmed if reassignment fails.
    /// </summary>
    public void UnassignForRevalidation()
    {
        DriverId = null;
        VehicleId = null;
        AssignmentType = null;
        RequiresManualAssignment = false;
        ManualAssignmentReason = null;
        Status = BookingStatus.Pending;
    }

    /// <summary>
    /// Cancelling releases the driver/vehicle (so they stop being considered in
    /// conflict checks — GetConflictScanAsync already excludes Cancelled bookings)
    /// but keeps every other field, including Price, intact for historical/reporting
    /// purposes. A Cancelled or Completed booking cannot be cancelled again — enforced
    /// by the caller (AdminBookingService), not here, so the failure can carry a
    /// specific, user-facing reason rather than a generic ArgumentException.
    /// </summary>
    public void Cancel(string? reason, Guid? cancelledByUserId, DateTime cancelledAt)
    {
        Status = BookingStatus.Cancelled;
        RideStatus = RideStatus.Cancelled;
        DriverId = null;
        VehicleId = null;
        AssignmentType = null;
        RequiresManualAssignment = false;
        ManualAssignmentReason = null;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        CancelledByUserId = cancelledByUserId;
        CancelledAt = cancelledAt;
    }

    /// <summary>
    /// Upcoming -&gt; OnTheWay. The precondition (not Cancelled/Completed,
    /// driver ownership, driver active) is enforced by the caller
    /// (DriverBookingService) so it can return a specific, user-facing 409
    /// reason — this guard is defense-in-depth, not the primary validation path.
    /// </summary>
    public void StartRide()
    {
        if (RideStatus != RideStatus.Upcoming)
            throw new InvalidOperationException("The ride has already started.");

        RideStatus = RideStatus.OnTheWay;
    }

    public void MarkPassengerPickedUp()
    {
        if (RideStatus != RideStatus.OnTheWay)
            throw new InvalidOperationException("Passenger can only be marked as picked up after the ride has started.");

        RideStatus = RideStatus.PassengerPickedUp;
    }

    /// <summary>Also moves the booking lifecycle to Completed — this is the only path that ever sets that status.</summary>
    public void CompleteRide()
    {
        if (RideStatus != RideStatus.PassengerPickedUp)
            throw new InvalidOperationException("The ride can only be completed after the passenger has been picked up.");

        RideStatus = RideStatus.Completed;
        Status = BookingStatus.Completed;
    }
}
