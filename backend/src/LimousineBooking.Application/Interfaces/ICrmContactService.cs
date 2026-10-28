using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmContactService
{
    Task<PagedResult<CrmContactResponse>> SearchAsync(CrmContactSearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmContactDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmContactOperationResult> CreateAsync(CreateCrmContactRequest request, CancellationToken cancellationToken = default);

    Task<CrmContactOperationResult> UpdateAsync(Guid id, UpdateCrmContactRequest request, CancellationToken cancellationToken = default);

    Task<CrmContactOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default);

    Task<CrmContactOperationResult> RecordContactedAsync(Guid id, DateTime contactedAtUtc, CancellationToken cancellationToken = default);

    Task<CrmContactOperationResult> ScheduleFollowUpAsync(Guid id, DateTime? followUpAtUtc, CancellationToken cancellationToken = default);
}
