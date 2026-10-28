using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmTaskService
{
    Task<PagedResult<CrmTaskResponse>> SearchAsync(CrmTaskSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmTaskOperationResult> CreateAsync(CreateCrmTaskRequest request, CancellationToken cancellationToken = default);

    Task<CrmTaskOperationResult> UpdateAsync(Guid id, UpdateCrmTaskRequest request, CancellationToken cancellationToken = default);

    Task<CrmTaskOperationResult> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmTaskOperationResult> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
