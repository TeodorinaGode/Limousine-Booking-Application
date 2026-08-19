using LimousineBooking.Domain.Entities;
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
