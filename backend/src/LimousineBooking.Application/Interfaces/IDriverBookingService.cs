using LimousineBooking.Application.Common;
using LimousineBooking.Application.Drivers;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// The authenticated driver's own bookings: dashboard, schedule, trip detail, and
/// ride-status transitions (Upcoming -&gt; OnTheWay -&gt; PassengerPickedUp -&gt; Completed).
/// Every method takes the driverId resolved from the JWT — never from the request.
/// </summary>
public interface IDriverBookingService
{
    Task<DriverDashboardResponse> GetDashboardAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<PagedResult<DriverBookingListItemResponse>> SearchAsync(Guid driverId, DriverBookingSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>Null if no such booking exists, or it belongs to a different driver.</summary>
    Task<DriverBookingDetailResponse?> GetByIdAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Upcoming -&gt; OnTheWay.</summary>
    Task<DriverBookingOperationResult> StartRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>OnTheWay -&gt; PassengerPickedUp.</summary>
    Task<DriverBookingOperationResult> MarkPassengerPickedUpAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>PassengerPickedUp -&gt; Completed. Also moves the booking's Status to Completed and triggers the customer completion notification.</summary>
    Task<DriverBookingOperationResult> CompleteRideAsync(Guid driverId, Guid bookingId, CancellationToken cancellationToken = default);
}
