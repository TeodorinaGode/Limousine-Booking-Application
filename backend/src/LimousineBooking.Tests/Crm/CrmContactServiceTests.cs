using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using Moq;
using DomainCrmActivity = LimousineBooking.Domain.Entities.CrmActivity;
using DomainCrmCompany = LimousineBooking.Domain.Entities.CrmCompany;
using DomainCrmContact = LimousineBooking.Domain.Entities.CrmContact;
using DomainUser = LimousineBooking.Domain.Entities.User;

namespace LimousineBooking.Tests.Crm;

public class CrmContactServiceTests
{
    private readonly Mock<ICrmContactRepository> _contactRepository = new();
    private readonly Mock<ICrmCompanyRepository> _companyRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICrmActivityRepository> _activityRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _emailMessageRepository = new();

    public CrmContactServiceTests()
    {
        _activityRepository
            .Setup(r => r.GetRecentForAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LimousineBooking.Domain.Entities.CrmActivity>());
        _emailMessageRepository
            .Setup(r => r.GetThreadIdsByMessageIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid>());
    }

    private CrmContactService CreateService() =>
        new(_contactRepository.Object, _companyRepository.Object, _userRepository.Object, _activityRepository.Object, _emailMessageRepository.Object);

    private static CreateCrmContactRequest ValidRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@grandhotel.example",
        Status = "NewProspect"
    };

    [Fact]
    public async Task Create_WithDuplicateEmail_IsRejected()
    {
        _contactRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(ValidRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmContactError.Duplicate, result.Error);
        _contactRepository.Verify(r => r.AddAsync(It.IsAny<DomainCrmContact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithUniqueEmail_Succeeds()
    {
        _contactRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().CreateAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.Equal("jane.doe@grandhotel.example", result.Contact!.Email);
    }

    [Fact]
    public async Task Create_WithOwnerThatIsNotAnAdministrator_IsRejected()
    {
        _contactRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var driverUser = new DomainUser("driver@example.com", "hash", "A", "B", UserRole.Driver);
        var ownerId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(driverUser);

        var request = ValidRequest();
        request.AssignedOwnerId = ownerId;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmContactError.Validation, result.Error);
    }

    [Fact]
    public async Task Create_WithNonExistentCompanyLink_IsRejected()
    {
        _contactRepository.Setup(r => r.HasDuplicateEmailAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var companyId = Guid.NewGuid();
        _companyRepository.Setup(r => r.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmCompany?)null);

        var request = ValidRequest();
        request.CompanyId = companyId;

        var result = await CreateService().CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmContactError.Validation, result.Error);
    }

    [Fact]
    public async Task Update_ToAnotherContactsEmail_IsRejected()
    {
        var contact = new DomainCrmContact("Jane", "Doe", "jane.doe@grandhotel.example");
        _contactRepository.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _contactRepository.Setup(r => r.HasDuplicateEmailAsync("taken@example.com", contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().UpdateAsync(contact.Id, new UpdateCrmContactRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "taken@example.com",
            Status = "Contacted"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmContactError.Duplicate, result.Error);
    }

    [Fact]
    public async Task Update_NonExistingContact_ReturnsNotFound()
    {
        _contactRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DomainCrmContact?)null);

        var result = await CreateService().UpdateAsync(Guid.NewGuid(), new UpdateCrmContactRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@grandhotel.example",
            Status = "Contacted"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(CrmContactError.NotFound, result.Error);
    }

    [Fact]
    public async Task SetArchived_True_ArchivesWithoutDeleting()
    {
        var contact = new DomainCrmContact("Jane", "Doe", "jane.doe@grandhotel.example");
        _contactRepository.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

        var result = await CreateService().SetArchivedAsync(contact.Id, true);

        Assert.True(result.Succeeded);
        Assert.True(result.Contact!.IsArchived);
        Assert.Equal(contact.Id, result.Contact.Id);
    }

    [Fact]
    public async Task GetDetailByIdAsync_PopulatesRecentActivities_IncludingTheConversationLinkForAnEmailSourcedOne()
    {
        var contact = new DomainCrmContact("Jane", "Doe", "jane.doe@grandhotel.example");
        var emailMessageId = Guid.NewGuid();
        var threadId = Guid.NewGuid();
        var activity = new DomainCrmActivity(
            CrmActivityType.EmailReceived, "Quote request", DateTime.UtcNow, Guid.NewGuid(),
            companyId: null, contactId: contact.Id, dealId: null, description: null, outcome: null, emailMessageId: emailMessageId);

        _contactRepository.Setup(r => r.GetDetailByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _activityRepository
            .Setup(r => r.GetRecentForAsync(null, contact.Id, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activity });
        _emailMessageRepository
            .Setup(r => r.GetThreadIdsByMessageIdsAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(emailMessageId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid> { [emailMessageId] = threadId });

        var detail = await CreateService().GetDetailByIdAsync(contact.Id);

        Assert.NotNull(detail);
        var recentActivity = Assert.Single(detail!.RecentActivities);
        Assert.Equal(emailMessageId, recentActivity.EmailMessageId);
        Assert.Equal(threadId, recentActivity.EmailThreadId);
    }
}
