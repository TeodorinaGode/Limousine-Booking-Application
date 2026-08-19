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
