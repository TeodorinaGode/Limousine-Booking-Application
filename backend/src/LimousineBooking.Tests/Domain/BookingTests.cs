using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class BookingTests
{
    private static Booking CreateValidBooking(decimal price = 180.00m) =>
        new(
            bookingReference: "LB-0001",
            customerFirstName: "Jane",
            customerLastName: "Doe",
            customerEmail: "jane.doe@example.com",
            customerPhone: "+41791234567",
            routeId: Guid.NewGuid(),
            travelDate: new DateOnly(2026, 9, 10),
            pickupTime: new TimeOnly(8, 0),
            pickupAddress: "Bahnhofplatz 1, Basel",
            passengerCount: 2,
            price: price,
            currency: "CHF");

    [Fact]
    public void Booking_Requires_CustomerFirstName()
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking("LB-0001", "", "Doe", "jane@example.com", "+41791234567",
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF"));
    }

    [Fact]
    public void Booking_Requires_BookingReference()
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking("", "Jane", "Doe", "jane@example.com", "+41791234567",
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Booking_PassengerCount_CannotBeZeroOrNegative(int passengerCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Booking("LB-0001", "Jane", "Doe", "jane@example.com", "+41791234567",
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", passengerCount, 180.00m, "CHF"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("no-domain@")]
    public void Booking_Requires_ValidEmailFormat(string invalidEmail)
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking("LB-0001", "Jane", "Doe", invalidEmail, "+41791234567",
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("phone#123")]
    public void Booking_Requires_ValidPhoneFormat(string invalidPhone)
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking("LB-0001", "Jane", "Doe", "jane@example.com", invalidPhone,
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", 2, 180.00m, "CHF"));
    }

    [Fact]
    public void Booking_Price_CannotBeNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Booking("LB-0001", "Jane", "Doe", "jane@example.com", "+41791234567",
                Guid.NewGuid(), new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
                "Bahnhofplatz 1, Basel", 2, -1m, "CHF"));
    }

    [Fact]
    public void Booking_CanExistWithoutDriver()
    {
        var booking = CreateValidBooking();

        Assert.Null(booking.DriverId);
    }

    [Fact]
    public void Booking_CanExistWithoutVehicle()
    {
        var booking = CreateValidBooking();

        Assert.Null(booking.VehicleId);
    }

    [Fact]
    public void ConfirmAutomaticAssignment_SetsDriverVehicleStatusAndAssignmentType()
    {
        var booking = CreateValidBooking();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        booking.ConfirmAutomaticAssignment(driverId, vehicleId);

        Assert.Equal(driverId, booking.DriverId);
        Assert.Equal(vehicleId, booking.VehicleId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(AssignmentType.Automatic, booking.AssignmentType);
        Assert.False(booking.RequiresManualAssignment);
        Assert.Null(booking.ManualAssignmentReason);
    }

    [Fact]
    public void MarkRequiresManualAssignment_LeavesStatusAndDriverUntouched()
    {
        var booking = CreateValidBooking();

        booking.MarkRequiresManualAssignment("No eligible driver was found.");

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.DriverId);
        Assert.Null(booking.VehicleId);
        Assert.True(booking.RequiresManualAssignment);
        Assert.Equal("No eligible driver was found.", booking.ManualAssignmentReason);
    }

    [Fact]
    public void MarkRequiresManualAssignment_RequiresANonEmptyReason()
    {
        var booking = CreateValidBooking();

        Assert.Throws<ArgumentException>(() => booking.MarkRequiresManualAssignment(""));
    }

    [Fact]
    public void RequiresManualAssignment_DefaultsToFalse()
    {
        var booking = CreateValidBooking();

        Assert.False(booking.RequiresManualAssignment);
        Assert.Null(booking.AssignmentType);
    }

    [Fact]
    public void ConfirmManualAssignment_SetsDriverVehicleStatusAndAssignmentType()
    {
        var booking = CreateValidBooking();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        booking.ConfirmManualAssignment(driverId, vehicleId);

        Assert.Equal(driverId, booking.DriverId);
        Assert.Equal(vehicleId, booking.VehicleId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(AssignmentType.Manual, booking.AssignmentType);
        Assert.False(booking.RequiresManualAssignment);
    }

    [Fact]
    public void UnassignForRevalidation_ClearsAssignmentAndResetsStatusToPending()
    {
        var booking = CreateValidBooking();
        booking.ConfirmAutomaticAssignment(Guid.NewGuid(), Guid.NewGuid());

        booking.UnassignForRevalidation();

        Assert.Null(booking.DriverId);
        Assert.Null(booking.VehicleId);
        Assert.Null(booking.AssignmentType);
        Assert.False(booking.RequiresManualAssignment);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public void UpdateDetails_ReplacesEditableFieldsIncludingPrice()
    {
        var booking = CreateValidBooking(price: 180.00m);
        var newRouteId = Guid.NewGuid();

        booking.UpdateDetails(
            newRouteId, new DateOnly(2026, 10, 1), new TimeOnly(9, 30), "Neue Adresse 5, Zurich", 3,
            "Janet", "Doey", "janet.doey@example.com", "+41791234999", "Extra luggage", 250.00m, "EUR");

        Assert.Equal(newRouteId, booking.RouteId);
        Assert.Equal(new DateOnly(2026, 10, 1), booking.TravelDate);
        Assert.Equal(new TimeOnly(9, 30), booking.PickupTime);
        Assert.Equal("Neue Adresse 5, Zurich", booking.PickupAddress);
        Assert.Equal(3, booking.PassengerCount);
        Assert.Equal("Janet", booking.CustomerFirstName);
        Assert.Equal(250.00m, booking.Price);
        Assert.Equal("EUR", booking.Currency);
    }

    [Fact]
    public void UpdateDetails_InvalidEmail_Throws()
    {
        var booking = CreateValidBooking();

        Assert.Throws<ArgumentException>(() => booking.UpdateDetails(
            Guid.NewGuid(), new DateOnly(2026, 10, 1), new TimeOnly(9, 30), "Address", 2,
            "Jane", "Doe", "not-an-email", "+41791234567", null, 180.00m, "CHF"));
    }

    [Fact]
    public void Cancel_SetsStatusAndReleasesDriverAndVehicle_ButKeepsPrice()
    {
        var booking = CreateValidBooking(price: 180.00m);
        booking.ConfirmAutomaticAssignment(Guid.NewGuid(), Guid.NewGuid());
        var adminUserId = Guid.NewGuid();
        var cancelledAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        booking.Cancel("Customer requested cancellation", adminUserId, cancelledAt);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Null(booking.DriverId);
        Assert.Null(booking.VehicleId);
        Assert.Null(booking.AssignmentType);
        Assert.Equal(180.00m, booking.Price);
        Assert.Equal("Customer requested cancellation", booking.CancellationReason);
        Assert.Equal(adminUserId, booking.CancelledByUserId);
        Assert.Equal(cancelledAt, booking.CancelledAt);
    }

    [Fact]
    public void RideStatus_DefaultsToUpcoming()
    {
        var booking = CreateValidBooking();

        Assert.Equal(RideStatus.Upcoming, booking.RideStatus);
    }

    [Fact]
    public void StartRide_MovesUpcomingToOnTheWay()
    {
        var booking = CreateValidBooking();

        booking.StartRide();

        Assert.Equal(RideStatus.OnTheWay, booking.RideStatus);
    }

    [Fact]
    public void StartRide_WhenAlreadyStarted_Throws()
    {
        var booking = CreateValidBooking();
        booking.StartRide();

        Assert.Throws<InvalidOperationException>(() => booking.StartRide());
    }

    [Fact]
    public void MarkPassengerPickedUp_MovesOnTheWayToPassengerPickedUp()
    {
        var booking = CreateValidBooking();
        booking.StartRide();

        booking.MarkPassengerPickedUp();

        Assert.Equal(RideStatus.PassengerPickedUp, booking.RideStatus);
    }

    [Fact]
    public void MarkPassengerPickedUp_BeforeRideStarted_Throws()
    {
        var booking = CreateValidBooking();

        Assert.Throws<InvalidOperationException>(() => booking.MarkPassengerPickedUp());
    }

    [Fact]
    public void MarkPassengerPickedUp_WhenAlreadyPickedUp_Throws()
    {
        var booking = CreateValidBooking();
        booking.StartRide();
        booking.MarkPassengerPickedUp();

        Assert.Throws<InvalidOperationException>(() => booking.MarkPassengerPickedUp());
    }

    [Fact]
    public void CompleteRide_MovesPassengerPickedUpToCompleted_AndAlsoCompletesBookingStatus()
    {
        var booking = CreateValidBooking();
        booking.StartRide();
        booking.MarkPassengerPickedUp();

        booking.CompleteRide();

        Assert.Equal(RideStatus.Completed, booking.RideStatus);
        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [Fact]
    public void CompleteRide_BeforePickup_Throws()
    {
        var booking = CreateValidBooking();
        booking.StartRide();

        Assert.Throws<InvalidOperationException>(() => booking.CompleteRide());
    }

    [Fact]
    public void CompleteRide_WhenAlreadyCompleted_Throws()
    {
        var booking = CreateValidBooking();
        booking.StartRide();
        booking.MarkPassengerPickedUp();
        booking.CompleteRide();

        Assert.Throws<InvalidOperationException>(() => booking.CompleteRide());
    }

    [Fact]
    public void Cancel_AlsoSetsRideStatusToCancelled()
    {
        var booking = CreateValidBooking();
        booking.ConfirmAutomaticAssignment(Guid.NewGuid(), Guid.NewGuid());

        booking.Cancel("Customer requested cancellation", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(RideStatus.Cancelled, booking.RideStatus);
    }

    [Fact]
    public void Booking_RoutePriceChange_DoesNotAffectExistingBookingPrice()
    {
        var route = new Route("Basel", "Zurich", 60, 180.00m, "CHF");
        var booking = new Booking(
            "LB-0001", "Jane", "Doe", "jane@example.com", "+41791234567",
            route.Id, new DateOnly(2026, 9, 10), new TimeOnly(8, 0),
            "Bahnhofplatz 1, Basel", 2, route.Price, "CHF");

        route.UpdatePrice(250.00m);

        Assert.Equal(180.00m, booking.Price);
        Assert.Equal(250.00m, route.Price);
    }
}
