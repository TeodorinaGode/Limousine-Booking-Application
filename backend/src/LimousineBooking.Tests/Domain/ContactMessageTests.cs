using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class ContactMessageTests
{
    private static ContactMessage MakeMessage() =>
        new("Jane Doe", "jane.doe@example.com", "+41791234567", "Airport transfer", "I would like to book an airport transfer for next week.");

    [Fact]
    public void NewContactMessage_StartsPending()
    {
        var message = MakeMessage();

        Assert.Equal(ContactMessageStatus.Pending, message.Status);
    }

    [Fact]
    public void Constructor_MissingName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactMessage("", "jane@example.com", null, "Subject", "A message long enough."));
    }

    [Fact]
    public void Constructor_InvalidEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactMessage("Jane", "not-an-email", null, "Subject", "A message long enough."));
    }

    [Fact]
    public void Constructor_InvalidPhone_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactMessage("Jane", "jane@example.com", "abc", "Subject", "A message long enough."));
    }

    [Fact]
    public void Constructor_NullPhone_IsAllowed()
    {
        var message = new ContactMessage("Jane", "jane@example.com", null, "Subject", "A message long enough.");

        Assert.Null(message.Phone);
    }

    [Fact]
    public void Constructor_MissingSubject_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactMessage("Jane", "jane@example.com", null, "", "A message long enough."));
    }

    [Fact]
    public void Constructor_MissingMessage_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactMessage("Jane", "jane@example.com", null, "Subject", ""));
    }

    [Fact]
    public void MarkSent_SetsStatusAndSentAt()
    {
        var message = MakeMessage();
        var sentAt = DateTime.UtcNow;

        message.MarkSent(sentAt);

        Assert.Equal(ContactMessageStatus.Sent, message.Status);
        Assert.Equal(sentAt, message.SentAt);
        Assert.Null(message.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_SetsStatusAndErrorMessage()
    {
        var message = MakeMessage();

        message.MarkFailed("SMTP unreachable");

        Assert.Equal(ContactMessageStatus.Failed, message.Status);
        Assert.Equal("SMTP unreachable", message.ErrorMessage);
    }

    [Theory]
    [InlineData("Phone")]
    [InlineData("email")]
    [InlineData(null)]
    public void Constructor_ValidOrMissingPreferredContactMethod_IsAccepted(string? method)
    {
        var message = new ContactMessage("Jane", "jane@example.com", null, "Subject", "A message long enough.", method, null);

        if (method is null)
            Assert.Null(message.PreferredContactMethod);
        else
            Assert.Equal(method, message.PreferredContactMethod);
    }

    [Fact]
    public void Constructor_InvalidPreferredContactMethod_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ContactMessage("Jane", "jane@example.com", null, "Subject", "A message long enough.", "Carrier Pigeon", null));
    }

    [Fact]
    public void Constructor_PreferredDate_IsStoredAsGiven()
    {
        var preferredDate = new DateOnly(2026, 9, 1);

        var message = new ContactMessage("Jane", "jane@example.com", null, "Subject", "A message long enough.", null, preferredDate);

        Assert.Equal(preferredDate, message.PreferredDate);
    }
}
