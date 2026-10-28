using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface IAdministratorDirectoryService
{
    Task<IReadOnlyList<AdministratorSummaryResponse>> GetAdministratorsAsync(CancellationToken cancellationToken = default);
}
