using Ganss.Xss;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Sanitizes synced/rendered email HTML before it ever reaches a response DTO
/// — scripts, inline event handlers, and other active content are stripped.
/// Remote images are deliberately left as-is at this layer (the frontend
/// gates them behind an explicit "Load images" click — see
/// EmailConversationView — since blocking them here would also break the
/// "load images" affordance).
/// </summary>
public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new Ganss.Xss.HtmlSanitizer();
        _sanitizer.AllowDataAttributes = false;
    }

    public string Sanitize(string html) => string.IsNullOrEmpty(html) ? html : _sanitizer.Sanitize(html);
}
