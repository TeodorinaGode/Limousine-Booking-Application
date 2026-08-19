using LimousineBooking.Application.Common;
using LimousineBooking.Application.Drivers;

namespace LimousineBooking.Application.Interfaces;

public interface IDriverService
{
    Task<PagedResult<DriverResponse>> SearchAsync(DriverSearchQuery query, CancellationToken cancellationToken = default);

    Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DriverOperationResult> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken = default);

    Task<DriverOperationResult> UpdateAsync(Guid id, UpdateDriverRequest request, CancellationToken cancellationToken = default);

    Task<DriverOperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<DriverOperationResult> ResetPasswordAsync(Guid id, ResetDriverPasswordRequest request, CancellationToken cancellationToken = default);
}
