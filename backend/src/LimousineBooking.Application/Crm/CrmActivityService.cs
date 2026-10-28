using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainCrmActivity = LimousineBooking.Domain.Entities.CrmActivity;

namespace LimousineBooking.Application.Crm;

public class CrmActivityService : ICrmActivityService
{
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmDealRepository _dealRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICrmEmailMessageRepository _emailMessageRepository;

    public CrmActivityService(
        ICrmActivityRepository activityRepository,
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        ICrmDealRepository dealRepository,
        IUserRepository userRepository,
        ICrmEmailMessageRepository emailMessageRepository)
    {
        _activityRepository = activityRepository;
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _dealRepository = dealRepository;
        _userRepository = userRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<PagedResult<CrmActivityResponse>> SearchAsync(CrmActivitySearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _activityRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<CrmActivityResponse>
        {
            Items = await CrmActivityResponseMapper.ToResponsesAsync(items, _emailMessageRepository, cancellationToken),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmActivityOperationResult> CreateAsync(Guid loggedByUserId, CreateCrmActivityRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CrmActivityType>(request.ActivityType, true, out var activityType))
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, "A valid activity type is required.");

        if (request.CompanyId.HasValue && await _companyRepository.GetByIdAsync(request.CompanyId.Value, cancellationToken) is null)
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, "Company not found.");

        if (request.ContactId.HasValue && await _contactRepository.GetByIdAsync(request.ContactId.Value, cancellationToken) is null)
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, "Contact not found.");

        if (request.DealId.HasValue && await _dealRepository.GetByIdAsync(request.DealId.Value, cancellationToken) is null)
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, "Deal not found.");

        var loggedBy = await _userRepository.GetByIdAsync(loggedByUserId, cancellationToken);
        if (loggedBy is null)
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, "Logging user not found.");

        DomainCrmActivity activity;
        try
        {
            activity = new DomainCrmActivity(
                activityType, request.Subject, request.OccurredAt ?? DateTime.UtcNow, loggedByUserId,
                request.CompanyId, request.ContactId, request.DealId, request.Description, request.Outcome);
        }
        catch (ArgumentException ex)
        {
            return CrmActivityOperationResult.Failure(CrmActivityError.Validation, ex.Message);
        }

        await _activityRepository.AddAsync(activity, cancellationToken);
        await _activityRepository.SaveChangesAsync(cancellationToken);

        // A newly created activity can never have an EmailMessageId (only the
        // sync/send flows ever set that internally), so there's no thread to resolve.
        // loggedBy is passed explicitly because activity.User isn't loaded on a
        // freshly constructed entity — there's no navigation-fixup from a bare Add().
        return CrmActivityOperationResult.Success(CrmActivityResponseMapper.ToResponse(activity, CrmActivityResponseMapper.EmptyThreadIdLookup, loggedBy));
    }
}
