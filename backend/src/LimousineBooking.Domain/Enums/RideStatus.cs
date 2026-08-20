namespace LimousineBooking.Domain.Enums;

/// <summary>
/// Trip progress, independent of <see cref="BookingStatus"/> (the booking
/// lifecycle) — a Confirmed booking can be Upcoming, OnTheWay, PassengerPickedUp,
/// or Completed; the two are never conflated.
/// </summary>
public enum RideStatus
{
    Upcoming,
    OnTheWay,
    PassengerPickedUp,
    Completed,
    Cancelled
}
