using LimousineBooking.Application.Contact;
using LimousineBooking.Application.Interfaces;
using Moq;
using Xunit;
using DomainContactMessage = LimousineBooking.Domain.Entities.ContactMessage;

namespace LimousineBooking.Tests.Contact;

public class PublicContactServiceTests
{
    private readonly Mock<IContactMessageRepository> _contactMessageRepository = new();

    public PublicContactServiceTests()
    {
        _contactMessageRepository.Setup(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _contactMessageRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private PublicContactService CreateService() => new(_contactMessageRepository.Object);

    private static ContactRequest MakeRequest(Action<ContactRequest>? mutate = null)
    {
        var request = new ContactRequest
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            Phone = "+41791234567",
            Subject = "Airport transfer",
            Message = "I would like to book an airport transfer for next week."
        };
        mutate?.Invoke(request);
        return request;
    }

    [Fact]
    public async Task SubmitAsync_ValidRequest_PersistsAndSucceeds()
    {
        var result = await CreateService().SubmitAsync(MakeRequest());

        Assert.True(result.Succeeded);
        _contactMessageRepository.Verify(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactMessageRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_TrimsFieldsBeforePersisting()
    {
        DomainContactMessage? captured = null;
        _contactMessageRepository.Setup(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<DomainContactMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        await CreateService().SubmitAsync(MakeRequest(r => r.Name = "  Jane Doe  "));

        Assert.Equal("Jane Doe", captured!.Name);
    }

    [Fact]
    public async Task SubmitAsync_InvalidEmail_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Email = "not-an-email"));

        Assert.False(result.Succeeded);
        _contactMessageRepository.Verify(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_MissingName_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Name = ""));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_MessageTooShort_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Message = "short"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_MessageTooLong_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Message = new string('a', 2001)));

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("Hello <b>world</b>")]
    public async Task SubmitAsync_MessageContainingHtml_Fails(string message)
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Message = message + " padding to satisfy min length"));

        Assert.False(result.Succeeded);
        _contactMessageRepository.Verify(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_NoPhoneProvided_Succeeds()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Phone = null));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_InvalidPhone_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.Phone = "abc"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_PreferredContactMethodAndDate_ArePersisted()
    {
        DomainContactMessage? captured = null;
        _contactMessageRepository.Setup(r => r.AddAsync(It.IsAny<DomainContactMessage>(), It.IsAny<CancellationToken>()))
            .Callback<DomainContactMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);
        var preferredDate = new DateOnly(2026, 9, 1);

        var result = await CreateService().SubmitAsync(MakeRequest(r =>
        {
            r.PreferredContactMethod = "Phone";
            r.PreferredDate = preferredDate;
        }));

        Assert.True(result.Succeeded);
        Assert.Equal("Phone", captured!.PreferredContactMethod);
        Assert.Equal(preferredDate, captured.PreferredDate);
    }

    [Fact]
    public async Task SubmitAsync_InvalidPreferredContactMethod_Fails()
    {
        var result = await CreateService().SubmitAsync(MakeRequest(r => r.PreferredContactMethod = "Carrier Pigeon"));

        Assert.False(result.Succeeded);
    }
}
