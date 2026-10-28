using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;

namespace LimousineBooking.Application.Interfaces;

public interface ICrmCompanyService
{
    Task<PagedResult<CrmCompanyResponse>> SearchAsync(CrmCompanySearchQuery query, CancellationToken cancellationToken = default);

    Task<CrmCompanyDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> CreateAsync(CreateCrmCompanyRequest request, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> UpdateAsync(Guid id, UpdateCrmCompanyRequest request, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> RecordContactedAsync(Guid id, DateTime contactedAtUtc, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> ScheduleFollowUpAsync(Guid id, DateTime? followUpAtUtc, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> LinkContactAsync(Guid companyId, Guid contactId, bool isPrimary, CancellationToken cancellationToken = default);

    Task<CrmCompanyOperationResult> UnlinkContactAsync(Guid companyId, Guid contactId, CancellationToken cancellationToken = default);
}
