using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Xunit;

namespace LimousineBooking.Tests.Domain;

public class EmailSendOperationTests
{
    private static EmailSendOperation CreatePending() => new(
        Guid.NewGuid(), Guid.NewGuid(), "<abc@roi-limousinen.ch>",
        new[] { new EmailAddressValue("jane@example.com", "Jane Doe") }, null, null,
        "Hello", "<p>Hi</p>", null, null, null, null, Guid.NewGuid());

    [Fact]
    public void Constructor_WithNoRecipients_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new EmailSendOperation(Guid.NewGuid(), Guid.NewGuid(), "<abc@x.ch>", Array.Empty<EmailAddressValue>(), null, null, "Hi", "<p>Hi</p>", null, null, null, null, Guid.NewGuid()));
    }

    [Fact]
    public void MarkSubmitting_FromPending_Succeeds()
    {
        var operation = CreatePending();

        operation.MarkSubmitting();

        Assert.Equal(EmailSendStatus.Submitting, operation.Status);
    }

    [Fact]
    public void MarkSubmitting_WhenNotPending_Throws()
    {
        var operation = CreatePending();
        operation.MarkSubmitting();

        Assert.Throws<InvalidOperationException>(() => operation.MarkSubmitting());
    }

    [Fact]
    public void MarkAccepted_SetsProviderMessageIdAndClearsError()
    {
        var operation = CreatePending();
        operation.MarkSubmitting();
        operation.MarkFailed("transient");

        operation.MarkAccepted("gmail-message-id-123");

        Assert.Equal(EmailSendStatus.Accepted, operation.Status);
        Assert.Equal("gmail-message-id-123", operation.ProviderMessageId);
        Assert.Null(operation.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_SetsErrorMessage()
    {
        var operation = CreatePending();
        operation.MarkSubmitting();

        operation.MarkFailed("Gmail returned 500.");

        Assert.Equal(EmailSendStatus.Failed, operation.Status);
        Assert.Equal("Gmail returned 500.", operation.ErrorMessage);
    }
}
