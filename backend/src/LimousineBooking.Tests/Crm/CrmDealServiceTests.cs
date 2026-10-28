using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Moq;
using DomainCrmCompany = LimousineBooking.Domain.Entities.CrmCompany;
using DomainCrmDeal = LimousineBooking.Domain.Entities.CrmDeal;
using DomainCrmPipeline = LimousineBooking.Domain.Entities.CrmPipeline;
using DomainCrmPipelineStage = LimousineBooking.Domain.Entities.CrmPipelineStage;

namespace LimousineBooking.Tests.Crm;

public class CrmDealServiceTests
{
    private readonly Mock<ICrmDealRepository> _dealRepository = new();
    private readonly Mock<ICrmCompanyRepository> _companyRepository = new();
    private readonly Mock<ICrmContactRepository> _contactRepository = new();
    private readonly Mock<ICrmPipelineRepository> _pipelineRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICrmActivityRepository> _activityRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _emailMessageRepository = new();

    public CrmDealServiceTests()
    {
        _activityRepository
            .Setup(r => r.GetRecentForAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LimousineBooking.Domain.Entities.CrmActivity>());
        _emailMessageRepository
            .Setup(r => r.GetThreadIdsByMessageIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid>());
    }

    private CrmDealService CreateService() =>
        new(_dealRepository.Object, _companyRepository.Object, _contactRepository.Object, _pipelineRepository.Object, _userRepository.Object, _activityRepository.Object, _emailMessageRepository.Object);

    private static DomainCrmDeal CreateDeal(Guid pipelineId) =>
        new("Annual contract", Guid.NewGuid(), pipelineId, Guid.NewGuid(), 10000m, "CHF");

    [Fact]
    public async Task Create_WithNonExistentCompany_IsRejected()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmCompany?)null);

        var result = await CreateService().CreateAsync(new CreateCrmDealRequest { Name = "Deal", CompanyId = companyId, Value = 100m, Currency = "CHF" });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithNoDefaultPipelineConfigured_IsRejected()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(new DomainCrmCompany("Acme", CrmCompanyType.CorporateClient));
        _pipelineRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmPipeline?)null);

        var result = await CreateService().CreateAsync(new CreateCrmDealRequest { Name = "Deal", CompanyId = companyId, Value = 100m, Currency = "CHF" });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.Validation, result.Error);
    }

    [Fact]
    public async Task MoveStage_ToStageInADifferentPipeline_IsRejected()
    {
        var deal = CreateDeal(Guid.NewGuid());
        _dealRepository.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        var otherPipelineStage = new DomainCrmPipelineStage(Guid.NewGuid(), "Qualified", 1, false, false, 35);
        _pipelineRepository.Setup(r => r.GetStageByIdAsync(otherPipelineStage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherPipelineStage);

        var result = await CreateService().MoveStageAsync(deal.Id, new MoveCrmDealStageRequest { StageId = otherPipelineStage.Id });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.Validation, result.Error);
    }

    [Fact]
    public async Task MoveStage_ToUnknownStage_IsRejected()
    {
        var deal = CreateDeal(Guid.NewGuid());
        _dealRepository.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);
        _pipelineRepository.Setup(r => r.GetStageByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmPipelineStage?)null);

        var result = await CreateService().MoveStageAsync(deal.Id, new MoveCrmDealStageRequest { StageId = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.Validation, result.Error);
    }

    [Fact]
    public async Task MoveStage_IntoLostStage_WithoutReason_IsRejected()
    {
        var pipelineId = Guid.NewGuid();
        var deal = CreateDeal(pipelineId);
        _dealRepository.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        var lostStage = new DomainCrmPipelineStage(pipelineId, "Closed Lost", 9, false, true, 0);
        _pipelineRepository.Setup(r => r.GetStageByIdAsync(lostStage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lostStage);

        var result = await CreateService().MoveStageAsync(deal.Id, new MoveCrmDealStageRequest { StageId = lostStage.Id, LossReason = null });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.Validation, result.Error);
    }

    [Fact]
    public async Task MoveStage_IntoLostStage_WithReason_Succeeds_AndSetsClosedAt()
    {
        var pipelineId = Guid.NewGuid();
        var deal = CreateDeal(pipelineId);
        _dealRepository.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        var lostStage = new DomainCrmPipelineStage(pipelineId, "Closed Lost", 9, false, true, 0);
        _pipelineRepository.Setup(r => r.GetStageByIdAsync(lostStage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lostStage);

        var result = await CreateService().MoveStageAsync(deal.Id, new MoveCrmDealStageRequest { StageId = lostStage.Id, LossReason = "Chose a competitor" });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Deal!.ClosedAt);
        Assert.True(result.Deal.IsLost);
        Assert.Equal("Chose a competitor", result.Deal.LossReason);
    }

    [Fact]
    public async Task MoveStage_ToOpenStage_Succeeds_AndLeavesClosedAtNull()
    {
        var pipelineId = Guid.NewGuid();
        var deal = CreateDeal(pipelineId);
        _dealRepository.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        var qualifiedStage = new DomainCrmPipelineStage(pipelineId, "Qualified", 3, false, false, 35);
        _pipelineRepository.Setup(r => r.GetStageByIdAsync(qualifiedStage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(qualifiedStage);

        var result = await CreateService().MoveStageAsync(deal.Id, new MoveCrmDealStageRequest { StageId = qualifiedStage.Id });

        Assert.True(result.Succeeded);
        Assert.Null(result.Deal!.ClosedAt);
        Assert.Equal(35, result.Deal.Probability);
    }

    [Fact]
    public async Task MoveStage_NonExistingDeal_ReturnsNotFound()
    {
        _dealRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmDeal?)null);

        var result = await CreateService().MoveStageAsync(Guid.NewGuid(), new MoveCrmDealStageRequest { StageId = Guid.NewGuid() });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmDealError.NotFound, result.Error);
    }
}
