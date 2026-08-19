using LimousineBooking.Application.Bookings;

namespace LimousineBooking.Application.Interfaces;

public interface IPublicBookingService
{
    Task<IReadOnlyList<PublicRouteResponse>> GetActiveRoutesAsync(CancellationToken cancellationToken = default);

    Task<BookingOperationResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);
}
