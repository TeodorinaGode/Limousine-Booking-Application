using LimousineBooking.Application.Common;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using DomainCrmDeal = LimousineBooking.Domain.Entities.CrmDeal;
using DomainCrmDealContact = LimousineBooking.Domain.Entities.CrmDealContact;
using DomainCrmPipelineStage = LimousineBooking.Domain.Entities.CrmPipelineStage;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Application.Crm;

public class CrmDealService : ICrmDealService
{
    private readonly ICrmDealRepository _dealRepository;
    private readonly ICrmCompanyRepository _companyRepository;
    private readonly ICrmContactRepository _contactRepository;
    private readonly ICrmPipelineRepository _pipelineRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICrmActivityRepository _activityRepository;
    private readonly ICrmEmailMessageRepository _emailMessageRepository;

    public CrmDealService(
        ICrmDealRepository dealRepository,
        ICrmCompanyRepository companyRepository,
        ICrmContactRepository contactRepository,
        ICrmPipelineRepository pipelineRepository,
        IUserRepository userRepository,
        ICrmActivityRepository activityRepository,
        ICrmEmailMessageRepository emailMessageRepository)
    {
        _dealRepository = dealRepository;
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _pipelineRepository = pipelineRepository;
        _userRepository = userRepository;
        _activityRepository = activityRepository;
        _emailMessageRepository = emailMessageRepository;
    }

    public async Task<PagedResult<CrmDealResponse>> SearchAsync(CrmDealSearchQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _dealRepository.SearchAsync(query, cancellationToken);

        return new PagedResult<CrmDealResponse>
        {
            Items = items.Select(d => ToResponse(d, d.Owner)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CrmDealDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetDetailByIdAsync(id, cancellationToken);
        if (deal is null)
            return null;

        var recentActivities = await _activityRepository.GetRecentForAsync(null, null, id, 10, cancellationToken);

        return new CrmDealDetailResponse
        {
            RecentActivities = await CrmActivityResponseMapper.ToResponsesAsync(recentActivities, _emailMessageRepository, cancellationToken),
            Deal = ToResponse(deal, deal.Owner),
            Contacts = deal.DealContacts
                .Where(dc => dc.Contact is not null)
                .Select(dc => new CrmContactSummary
                {
                    Id = dc.Contact!.Id,
                    FirstName = dc.Contact.FirstName,
                    LastName = dc.Contact.LastName,
                    Email = dc.Contact.Email,
                    JobTitle = dc.Contact.JobTitle
                })
                .ToList(),
            OpenTasks = deal.Tasks
                .Select(t => new CrmTaskSummary
                {
                    Id = t.Id,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString()
                })
                .ToList()
        };
    }

    public async Task<CrmPipelineBoardResponse?> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var pipeline = await _pipelineRepository.GetDefaultAsync(cancellationToken);
        if (pipeline is null)
            return null;

        var deals = await _dealRepository.GetPipelineBoardAsync(pipeline.Id, cancellationToken);
        var dealsByStage = deals.GroupBy(d => d.StageId).ToDictionary(g => g.Key, g => g.ToList());

        return new CrmPipelineBoardResponse
        {
            PipelineId = pipeline.Id,
            PipelineName = pipeline.Name,
            Columns = pipeline.Stages
                .OrderBy(s => s.SortOrder)
                .Select(stage => new CrmPipelineBoardColumnResponse
                {
                    Stage = ToStageSummary(stage),
                    Deals = dealsByStage.TryGetValue(stage.Id, out var stageDeals)
                        ? stageDeals.Select(d => ToResponse(d, d.Owner)).ToList()
                        : Array.Empty<CrmDealResponse>()
                })
                .ToList()
        };
    }

    public async Task<CrmDealOperationResult> CreateAsync(CreateCrmDealRequest request, CancellationToken cancellationToken = default)
    {
        if (await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken) is null)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "Company not found.");

        var ownerCheck = await ValidateOwnerAsync(request.OwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        DomainCrmPipelineStage stage;
        if (request.StageId.HasValue)
        {
            var found = await _pipelineRepository.GetStageByIdAsync(request.StageId.Value, cancellationToken);
            if (found is null)
                return CrmDealOperationResult.Failure(CrmDealError.Validation, "Stage not found.");
            stage = found;
        }
        else
        {
            var pipeline = await _pipelineRepository.GetDefaultAsync(cancellationToken);
            var firstStage = pipeline?.Stages.OrderBy(s => s.SortOrder).FirstOrDefault();
            if (pipeline is null || firstStage is null)
                return CrmDealOperationResult.Failure(CrmDealError.Validation, "No default pipeline is configured.");
            stage = firstStage;
        }

        DomainCrmDeal deal;
        try
        {
            deal = new DomainCrmDeal(request.Name, request.CompanyId, stage.PipelineId, stage.Id, request.Value, request.Currency);
            deal.SetProbability(stage.DefaultProbability);
            deal.UpdateDetails(request.Name, request.Description, request.DealType, request.Value, request.Currency,
                request.ExpectedCloseDate, request.OwnerId, request.NextAction, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmDealOperationResult.Failure(CrmDealError.Validation, ex.Message);
        }

        await _dealRepository.AddAsync(deal, cancellationToken);
        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, ownerCheck.Owner, stage));
    }

    public async Task<CrmDealOperationResult> UpdateAsync(Guid id, UpdateCrmDealRequest request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(id, cancellationToken);
        if (deal is null)
            return CrmDealOperationResult.Failure(CrmDealError.NotFound, "Deal not found.");

        var ownerCheck = await ValidateOwnerAsync(request.OwnerId, cancellationToken);
        if (ownerCheck.Error is not null)
            return ownerCheck.Error;

        try
        {
            deal.UpdateDetails(request.Name, request.Description, request.DealType, request.Value, request.Currency,
                request.ExpectedCloseDate, request.OwnerId, request.NextAction, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return CrmDealOperationResult.Failure(CrmDealError.Validation, ex.Message);
        }

        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, ownerCheck.Owner, deal.Stage));
    }

    public async Task<CrmDealOperationResult> MoveStageAsync(Guid id, MoveCrmDealStageRequest request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(id, cancellationToken);
        if (deal is null)
            return CrmDealOperationResult.Failure(CrmDealError.NotFound, "Deal not found.");

        var stage = await _pipelineRepository.GetStageByIdAsync(request.StageId, cancellationToken);
        if (stage is null)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "Stage not found.");
        if (stage.PipelineId != deal.PipelineId)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "The target stage must belong to the deal's pipeline.");

        try
        {
            deal.MoveToStage(stage.Id, stage.DefaultProbability, stage.IsWon, stage.IsLost, request.LossReason);
        }
        catch (ArgumentException ex)
        {
            return CrmDealOperationResult.Failure(CrmDealError.Validation, ex.Message);
        }

        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, deal.Owner, stage));
    }

    public async Task<CrmDealOperationResult> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(id, cancellationToken);
        if (deal is null)
            return CrmDealOperationResult.Failure(CrmDealError.NotFound, "Deal not found.");

        if (isArchived) deal.Archive(); else deal.Unarchive();

        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, deal.Owner, deal.Stage));
    }

    public async Task<CrmDealOperationResult> LinkContactAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal is null)
            return CrmDealOperationResult.Failure(CrmDealError.NotFound, "Deal not found.");

        if (await _contactRepository.GetByIdAsync(contactId, cancellationToken) is null)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "Contact not found.");

        if (await _dealRepository.GetDealContactLinkAsync(dealId, contactId, cancellationToken) is not null)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "This contact is already linked to this deal.");

        await _dealRepository.AddDealContactAsync(new DomainCrmDealContact(dealId, contactId), cancellationToken);
        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, deal.Owner, deal.Stage));
    }

    public async Task<CrmDealOperationResult> UnlinkContactAsync(Guid dealId, Guid contactId, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);
        if (deal is null)
            return CrmDealOperationResult.Failure(CrmDealError.NotFound, "Deal not found.");

        var link = await _dealRepository.GetDealContactLinkAsync(dealId, contactId, cancellationToken);
        if (link is null)
            return CrmDealOperationResult.Failure(CrmDealError.Validation, "This contact is not linked to this deal.");

        await _dealRepository.RemoveDealContactAsync(link, cancellationToken);
        await _dealRepository.SaveChangesAsync(cancellationToken);

        return CrmDealOperationResult.Success(ToResponse(deal, deal.Owner, deal.Stage));
    }

    private async Task<(DomainUser? Owner, CrmDealOperationResult? Error)> ValidateOwnerAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (!ownerId.HasValue)
            return (null, null);

        var owner = await _userRepository.GetByIdAsync(ownerId.Value, cancellationToken);
        if (owner is null || owner.Role != UserRole.Administrator)
            return (null, CrmDealOperationResult.Failure(CrmDealError.Validation, "Owner must be an existing administrator."));

        return (owner, null);
    }

    private static CrmPipelineStageSummary ToStageSummary(DomainCrmPipelineStage stage) => new()
    {
        Id = stage.Id,
        Name = stage.Name,
        SortOrder = stage.SortOrder,
        IsWon = stage.IsWon,
        IsLost = stage.IsLost,
        DefaultProbability = stage.DefaultProbability
    };

    private static CrmDealResponse ToResponse(DomainCrmDeal deal, DomainUser? owner, DomainCrmPipelineStage? stageOverride = null)
    {
        var stage = stageOverride ?? deal.Stage;

        return new CrmDealResponse
        {
            Id = deal.Id,
            Name = deal.Name,
            Company = deal.Company is null
                ? new CrmCompanySummary()
                : new CrmCompanySummary { Id = deal.Company.Id, Name = deal.Company.Name, CompanyType = deal.Company.CompanyType.ToString() },
            Description = deal.Description,
            DealType = deal.DealType,
            Value = deal.Value,
            Currency = deal.Currency,
            PipelineId = deal.PipelineId,
            StageId = deal.StageId,
            StageName = stage?.Name ?? string.Empty,
            IsWon = stage?.IsWon ?? false,
            IsLost = stage?.IsLost ?? false,
            Probability = deal.Probability,
            ExpectedCloseDate = deal.ExpectedCloseDate,
            Owner = owner is null
                ? null
                : new CrmOwnerSummary { Id = owner.Id, FirstName = owner.FirstName, LastName = owner.LastName, Email = owner.Email },
            NextAction = deal.NextAction,
            ClosedAt = deal.ClosedAt,
            LossReason = deal.LossReason,
            Notes = deal.Notes,
            IsArchived = deal.IsArchived,
            CreatedAt = deal.CreatedAt,
            UpdatedAt = deal.UpdatedAt
        };
    }
}
