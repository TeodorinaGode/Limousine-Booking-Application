using LimousineBooking.Application.Email;
using Xunit;

namespace LimousineBooking.Tests.Email;

public class TemplateVariableRendererTests
{
    [Fact]
    public void Render_SubstitutesKnownTokensAndReportsNoUnresolved()
    {
        var values = new Dictionary<string, string> { ["contact.firstName"] = "Jane" };

        var (rendered, unresolved) = TemplateVariableRenderer.Render("Dear {{contact.firstName}},", values);

        Assert.Equal("Dear Jane,", rendered);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void Render_LeavesAnUnknownTokenExactlyAsWrittenAndReportsIt()
    {
        var (rendered, unresolved) = TemplateVariableRenderer.Render("Dear {{contact.firstName}},", new Dictionary<string, string>());

        Assert.Equal("Dear {{contact.firstName}},", rendered);
        Assert.Equal(new[] { "contact.firstName" }, unresolved);
    }

    [Fact]
    public void Render_HtmlEscapesTheSubstitutedValue()
    {
        var values = new Dictionary<string, string> { ["company.name"] = "<script>alert(1)</script> & Co" };

        var (rendered, unresolved) = TemplateVariableRenderer.Render("{{company.name}}", values);

        Assert.DoesNotContain("<script>", rendered);
        Assert.Contains("&lt;script&gt;", rendered);
        Assert.Contains("&amp;", rendered);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void Render_ReportsEachDistinctUnresolvedTokenOnce()
    {
        var (_, unresolved) = TemplateVariableRenderer.Render("{{deal.name}} and {{deal.name}} again, plus {{deal.value}}", new Dictionary<string, string>());

        Assert.Equal(new[] { "deal.name", "deal.name", "deal.value" }, unresolved);
    }
}
