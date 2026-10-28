using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmActivityService
{
    Task<PagedResult<CrmActivityResponse>> SearchAsync(CrmActivitySearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmActivityOperationResult> CreateAsync(Guid loggedByUserId, CreateCrmActivityRequest request, CancellationToken cancellationToken = default);
}
