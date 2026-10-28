using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class CrmInboxServiceTests
{
    private readonly Mock<ICrmEmailThreadRepository> _threadRepository = new();
    private readonly Mock<ICrmEmailMessageRepository> _messageRepository = new();
    private readonly Mock<ICrmCompanyRepository> _companyRepository = new();
    private readonly Mock<ICrmContactRepository> _contactRepository = new();
    private readonly Mock<ICrmDealRepository> _dealRepository = new();
    private readonly Mock<IEmailProviderConnectionRepository> _connectionRepository = new();
    private readonly Mock<IEmailAccessTokenProvider> _accessTokenProvider = new();
    private readonly Mock<IEmailProviderClientFactory> _clientFactory = new();
    private readonly Mock<IEmailProviderClient> _providerClient = new();
    private readonly Mock<IHtmlSanitizerService> _htmlSanitizer = new();

    public CrmInboxServiceTests()
    {
        _clientFactory.Setup(f => f.GetClient(It.IsAny<EmailProvider>())).Returns(_providerClient.Object);
        _htmlSanitizer.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(html => html);
    }

    private CrmInboxService CreateService() => new(
        _threadRepository.Object,
        _messageRepository.Object,
        _companyRepository.Object,
        _contactRepository.Object,
        _dealRepository.Object,
        _connectionRepository.Object,
        _accessTokenProvider.Object,
        _clientFactory.Object,
        _htmlSanitizer.Object);

    private static CrmEmailThread NewThread() =>
        new(Guid.NewGuid(), "provider-thread-1", "Quote request", DateTime.UtcNow);

    [Fact]
    public async Task GetThreadDetailAsync_WhenThreadDoesNotExist_ReturnsNull()
    {
        _threadRepository.Setup(r => r.GetDetailByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);

        var result = await CreateService().GetThreadDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SetReadAsync_MarksTheThreadReadAndPersists()
    {
        var thread = NewThread();
        thread.MarkUnread();
        _threadRepository.Setup(r => r.GetDetailByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);

        var result = await CreateService().SetReadAsync(thread.Id, true);

        Assert.True(result.Succeeded);
        Assert.False(result.Thread!.HasUnread);
        _threadRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetReadAsync_WhenThreadDoesNotExist_ReturnsNotFound()
    {
        _threadRepository.Setup(r => r.GetDetailByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThread?)null);

        var result = await CreateService().SetReadAsync(Guid.NewGuid(), true);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.NotFound, result.Error);
    }

    [Fact]
    public async Task LinkCompanyAsync_WhenCompanyDoesNotExist_ReturnsValidationError()
    {
        var thread = NewThread();
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _companyRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmCompany?)null);

        var result = await CreateService().LinkCompanyAsync(thread.Id, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.Validation, result.Error);
        _threadRepository.Verify(r => r.AddCompanyLinkAsync(It.IsAny<CrmEmailThreadCompany>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkCompanyAsync_WhenAlreadyLinked_ReturnsDuplicateError()
    {
        var thread = NewThread();
        var company = new CrmCompany("Grand Hotel", CrmCompanyType.HotelResort);
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _threadRepository.Setup(r => r.GetCompanyLinkAsync(thread.Id, company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrmEmailThreadCompany(thread.Id, company.Id));

        var result = await CreateService().LinkCompanyAsync(thread.Id, company.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.Duplicate, result.Error);
    }

    [Fact]
    public async Task LinkCompanyAsync_WhenValid_AddsTheLinkAndReturnsTheReloadedThread()
    {
        var thread = NewThread();
        var company = new CrmCompany("Grand Hotel", CrmCompanyType.HotelResort);
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _companyRepository.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _threadRepository.Setup(r => r.GetCompanyLinkAsync(thread.Id, company.Id, It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThreadCompany?)null);
        _threadRepository.Setup(r => r.GetDetailByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);

        var result = await CreateService().LinkCompanyAsync(thread.Id, company.Id);

        Assert.True(result.Succeeded);
        _threadRepository.Verify(r => r.AddCompanyLinkAsync(It.Is<CrmEmailThreadCompany>(l => l.ThreadId == thread.Id && l.CompanyId == company.Id), It.IsAny<CancellationToken>()), Times.Once);
        _threadRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlinkCompanyAsync_WhenNotLinked_ReturnsValidationError()
    {
        var thread = NewThread();
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _threadRepository.Setup(r => r.GetCompanyLinkAsync(thread.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailThreadCompany?)null);

        var result = await CreateService().UnlinkCompanyAsync(thread.Id, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.Validation, result.Error);
    }

    [Fact]
    public async Task LinkContactAsync_WhenContactDoesNotExist_ReturnsValidationError()
    {
        var thread = NewThread();
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _contactRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmContact?)null);

        var result = await CreateService().LinkContactAsync(thread.Id, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.Validation, result.Error);
    }

    [Fact]
    public async Task LinkDealAsync_WhenDealDoesNotExist_ReturnsValidationError()
    {
        var thread = NewThread();
        _threadRepository.Setup(r => r.GetByIdAsync(thread.Id, It.IsAny<CancellationToken>())).ReturnsAsync(thread);
        _dealRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmDeal?)null);

        var result = await CreateService().LinkDealAsync(thread.Id, Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(CrmInboxError.Validation, result.Error);
    }

    [Fact]
    public async Task GetAttachmentAsync_WhenMessageDoesNotExist_ReturnsNull()
    {
        _messageRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrmEmailMessage?)null);

        var result = await CreateService().GetAttachmentAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachmentAsync_WhenAttachmentBelongsToTheMessage_StreamsItLiveFromTheProvider()
    {
        var connectionId = Guid.NewGuid();
        var message = new CrmEmailMessage(
            Guid.NewGuid(), connectionId, "provider-msg-1", EmailDirection.Inbound,
            "client@example.com", "Client", Array.Empty<LimousineBooking.Domain.Common.EmailAddressValue>(), null, null,
            "Quote", null, "Hi", DateTime.UtcNow, isRead: true, sendOperationId: null);
        var attachment = new CrmEmailAttachment(message.Id, "itinerary.pdf", "application/pdf", 2048, "provider-attachment-1");
        message.Attachments.Add(attachment);

        var connection = new EmailProviderConnection(EmailProvider.Gmail, "sales@roi-limousinen.ch", "encrypted", "access-token", DateTime.UtcNow.AddHours(1), Guid.NewGuid());

        _messageRepository.Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        _connectionRepository.Setup(r => r.GetByIdAsync(connectionId, It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _accessTokenProvider.Setup(p => p.GetFreshAccessTokenAsync(connection, It.IsAny<CancellationToken>())).ReturnsAsync("access-token");
        _providerClient.Setup(c => c.GetAttachmentAsync("access-token", "provider-msg-1", "provider-attachment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await CreateService().GetAttachmentAsync(message.Id, attachment.Id);

        Assert.NotNull(result);
        Assert.Equal("itinerary.pdf", result!.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
    }
}
