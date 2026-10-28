using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmDealService
{
    Task<PagedResult<CrmDealResponse>> SearchAsync(CrmDealSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmDealDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmPipelineBoardResponse?> GetBoardAsync(CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> CreateAsync(CreateCrmDealRequest request, CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> UpdateAsync(Guid id, UpdateCrmDealRequest request, CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> MoveStageAsync(Guid id, MoveCrmDealStageRequest request, CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> LinkContactAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default);

    Task<CrmDealOperationResult> UnlinkContactAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default);
}
