using LimousineBooking.Application.AdminAlerts;

namespace LimousineBooking.Application.Interfaces;

public interface IAdminAlertsService
{
    Task<AdminAlertsSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Records that this admin has just looked at the bell — resets the "new bookings" count going forward.</summary>
    Task MarkBookingsSeenAsync(Guid userId, CancellationToken cancellationToken = default);
}
