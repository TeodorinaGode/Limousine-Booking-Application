using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class CrmEmailTemplateServiceTests
{
    private readonly Mock<ICrmEmailTemplateRepository> _templateRepository = new();
    private readonly Mock<ICrmCompanyRepository> _companyRepository = new();
    private readonly Mock<ICrmContactRepository> _contactRepository = new();
    private readonly Mock<ICrmDealRepository> _dealRepository = new();

    private CrmEmailTemplateService CreateService() =>
        new(_templateRepository.Object, _companyRepository.Object, _contactRepository.Object, _dealRepository.Object);

    private static CreateCrmEmailTemplateRequest ValidRequest() => new()
    {
        Name = "Quote Follow-Up",
        Subject = "Following up on your quote",
        BodyHtml = "<p>Dear {{contact.firstName}},</p>",
        Category = "Follow-Up"
    };

    [Fact]
    public async Task CreateAsync_WithValidData_Succeeds()
    {
        var result = await CreateService().CreateAsync(ValidRequest(), Guid.NewGuid());

        Assert.True(result.Succeeded);
        Assert.Equal("Quote Follow-Up", result.Template!.Name);
        _templateRepository.Verify(r => r.AddAsync(It.IsAny<CrmEmailTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNoBody_ReturnsValidationError()
    {
        var request = ValidRequest();
        request.BodyHtml = "";

        var result = await CreateService().CreateAsync(request, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmEmailTemplateError.Validation, result.Error);
    }

    [Fact]
    public async Task SetArchivedAsync_WhenTemplateDoesNotExist_ReturnsNotFound()
    {
        _templateRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailTemplate?)null);

        var result = await CreateService().SetArchivedAsync(Guid.NewGuid(), true);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmEmailTemplateError.NotFound, result.Error);
    }

    [Fact]
    public async Task SetArchivedAsync_Archives_ThenUnarchives()
    {
        var template = new CrmEmailTemplate("Welcome", "Welcome!", "<p>Hi</p>", null, null, Guid.NewGuid());
        _templateRepository.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);

        var archived = await CreateService().SetArchivedAsync(template.Id, true);
        Assert.True(archived.Template!.IsArchived);

        var unarchived = await CreateService().SetArchivedAsync(template.Id, false);
        Assert.False(unarchived.Template!.IsArchived);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateDoesNotExist_ReturnsNull()
    {
        _templateRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailTemplate?)null);

        var result = await CreateService().RenderAsync(Guid.NewGuid(), null, null, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_WithAContactAndCompanySupplied_SubstitutesTheirRealValues()
    {
        var template = new CrmEmailTemplate(
            "Quote Follow-Up", "Following up with {{company.name}}", "<p>Dear {{contact.firstName}},</p>", null, null, Guid.NewGuid());
        var contact = new CrmContact("Jane", "Client", "jane@example.com");
        var company = new CrmCompany("Grand Hotel", CrmCompanyType.HotelResort);

        _templateRepository.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        _contactRepository.Setup(r => r.GetByIdAsync(contact.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var result = await CreateService().RenderAsync(template.Id, company.Id, contact.Id, null);

        Assert.NotNull(result);
        Assert.Equal("Following up with Grand Hotel", result!.Subject);
        Assert.Equal("<p>Dear Jane,</p>", result.BodyHtml);
        Assert.Empty(result.UnresolvedVariables);
    }

    [Fact]
    public async Task RenderAsync_WhenNoContextIsSupplied_ReportsEveryTokenAsUnresolvedAndLeavesThemLiteral()
    {
        var template = new CrmEmailTemplate(
            "Quote Follow-Up", "Following up with {{company.name}}", "<p>Dear {{contact.firstName}},</p>", null, null, Guid.NewGuid());
        _templateRepository.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);

        var result = await CreateService().RenderAsync(template.Id, null, null, null);

        Assert.NotNull(result);
        Assert.Equal("Following up with {{company.name}}", result!.Subject);
        Assert.Equal("<p>Dear {{contact.firstName}},</p>", result.BodyHtml);
        Assert.Contains("company.name", result.UnresolvedVariables);
        Assert.Contains("contact.firstName", result.UnresolvedVariables);
    }
}
