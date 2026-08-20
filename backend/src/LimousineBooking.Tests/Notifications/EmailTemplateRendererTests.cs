using LimousineBooking.Infrastructure.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace LimousineBooking.Tests.Notifications;

public class EmailTemplateRendererTests
{
    private static EmailTemplateRenderer CreateRenderer(string fromEmail = "ops@example.com") =>
        new(Options.Create(new EmailSettings { FromEmail = fromEmail }));

    private static Dictionary<string, string> BookingConfirmedFields() => new()
    {
        ["CustomerName"] = "Jane Doe",
        ["BookingReference"] = "LM-20261225-000123",
        ["Departure"] = "Basel",
        ["Destination"] = "Zurich",
        ["BookingDate"] = "25 December 2026",
        ["PickupTime"] = "14:00",
        ["PickupAddress"] = "Bahnhofplatz 1, Basel",
        ["PassengerCount"] = "2",
        ["Price"] = "180.00",
        ["Currency"] = "CHF",
        ["Status"] = "Confirmed"
    };

    [Fact]
    public void Render_ParsesSubjectFromFirstLine_WithPlaceholdersSubstituted()
    {
        var result = CreateRenderer().Render("BookingConfirmed", BookingConfirmedFields());

        Assert.Equal("Your limousine booking is confirmed — LM-20261225-000123", result.Subject);
    }

    [Fact]
    public void Render_SubstitutesPlaceholdersInBody()
    {
        var result = CreateRenderer().Render("BookingConfirmed", BookingConfirmedFields());

        Assert.Contains("Jane Doe", result.HtmlBody);
        Assert.Contains("LM-20261225-000123", result.HtmlBody);
        Assert.Contains("Basel", result.HtmlBody);
        Assert.Contains("CHF", result.HtmlBody);
        Assert.DoesNotContain("{{", result.HtmlBody);
    }

    [Fact]
    public void Render_WrapsContentInSharedLayout()
    {
        var result = CreateRenderer().Render("BookingConfirmed", BookingConfirmedFields());

        Assert.Contains("LIMOUSINE SERVICE", result.HtmlBody);
    }

    [Fact]
    public void Render_UsesConfiguredFromEmailAsContactAddress()
    {
        var result = CreateRenderer(fromEmail: "support@limo.example").Render("BookingConfirmed", BookingConfirmedFields());

        Assert.Contains("support@limo.example", result.HtmlBody);
    }

    [Fact]
    public void Render_ProducesPlainTextWithoutHtmlTags()
    {
        var result = CreateRenderer().Render("BookingConfirmed", BookingConfirmedFields());

        Assert.DoesNotContain("<", result.PlainTextBody);
        Assert.Contains("Jane Doe", result.PlainTextBody);
        Assert.Contains("LM-20261225-000123", result.PlainTextBody);
    }

    [Fact]
    public void Render_UnknownTemplate_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => CreateRenderer().Render("DoesNotExist", BookingConfirmedFields()));
    }

    [Theory]
    [InlineData("BookingPending")]
    [InlineData("DriverAssigned")]
    [InlineData("BookingReassigned")]
    [InlineData("BookingCancelled")]
    [InlineData("BookingCompleted")]
    [InlineData("DriverBookingAssigned")]
    [InlineData("DriverReassignedAway")]
    [InlineData("AdminManualAssignmentRequired")]
    public void Render_AllNotificationTemplates_RenderWithoutError(string templateName)
    {
        var fields = BookingConfirmedFields();
        fields["CustomerPhone"] = "+41791234567";
        fields["Notes"] = "(none)";
        fields["CancellationReason"] = "Customer requested cancellation";
        fields["Reason"] = "No driver available.";

        var result = CreateRenderer().Render(templateName, fields);

        Assert.False(string.IsNullOrWhiteSpace(result.Subject));
        Assert.False(string.IsNullOrWhiteSpace(result.HtmlBody));
        Assert.DoesNotContain("{{", result.Subject);
    }
}
