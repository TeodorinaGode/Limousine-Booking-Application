using System.Text;
using System.Text.RegularExpressions;
using Google.Apis.Gmail.v1.Data;
using LimousineBooking.Application.Email;
using LimousineBooking.Domain.Common;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Converts a Gmail API Message (its nested MIME Payload.Parts tree) into our
/// provider-agnostic ProviderMessageDetail. Gmail's SDK gives raw MIME parts,
/// not a "give me the HTML body" helper, so walking the tree and decoding
/// base64url bodies is genuinely necessary here.
/// </summary>
internal static class GmailMessageParser
{
    // Matches one comma-separated address segment of an RFC 5322 header value,
    // e.g. `"Jane Doe" <jane@example.com>` or a bare `jane@example.com`.
    private static readonly Regex AddressSegment = new(
        @"(?:""?(?<name>[^"",<]*)""?\s*)?<?(?<address>[^<>\s,]+@[^<>\s,]+)>?",
        RegexOptions.Compiled);

    public static ProviderMessageDetail Parse(Message message)
    {
        var headers = message.Payload?.Headers ?? new List<MessagePartHeader>();
        var from = ParseAddressList(HeaderValue(headers, "From")).FirstOrDefault();
        var attachments = new List<ProviderAttachmentSummary>();
        var (html, plain) = ExtractBodies(message.Payload, attachments);

        return new ProviderMessageDetail
        {
            ProviderMessageId = message.Id,
            ProviderThreadId = message.ThreadId,
            FromAddress = from?.Address ?? string.Empty,
            FromName = from?.Name,
            ToAddresses = ParseAddressList(HeaderValue(headers, "To")),
            CcAddresses = ParseAddressList(HeaderValue(headers, "Cc")),
            BccAddresses = ParseAddressList(HeaderValue(headers, "Bcc")),
            Subject = HeaderValue(headers, "Subject") ?? string.Empty,
            BodyHtml = html,
            BodyPlainText = plain,
            OccurredAtUtc = message.InternalDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).UtcDateTime
                : DateTime.UtcNow,
            IsUnread = message.LabelIds?.Contains("UNREAD") ?? false,
            Attachments = attachments
        };
    }

    public static string? HeaderValue(IList<MessagePartHeader> headers, string name) =>
        headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

    public static IReadOnlyList<EmailAddressValue> ParseAddressList(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
            return Array.Empty<EmailAddressValue>();

        var results = new List<EmailAddressValue>();
        foreach (Match match in AddressSegment.Matches(headerValue))
        {
            if (!match.Groups["address"].Success)
                continue;

            var address = match.Groups["address"].Value.Trim();
            var name = match.Groups["name"].Value.Trim();
            results.Add(new EmailAddressValue(address, string.IsNullOrWhiteSpace(name) ? null : name));
        }

        return results;
    }

    /// <summary>Recursively walks multipart/* parts collecting the first text/html and text/plain bodies found, plus any part with a filename (treated as an attachment).</summary>
    private static (string? Html, string? Plain) ExtractBodies(MessagePart? part, List<ProviderAttachmentSummary> attachments)
    {
        if (part is null)
            return (null, null);

        string? html = null;
        string? plain = null;

        if (!string.IsNullOrEmpty(part.Filename) && part.Body?.AttachmentId is not null)
        {
            attachments.Add(new ProviderAttachmentSummary
            {
                ProviderAttachmentId = part.Body.AttachmentId,
                FileName = part.Filename,
                ContentType = part.MimeType ?? "application/octet-stream",
                SizeBytes = part.Body.Size ?? 0
            });
            return (null, null);
        }

        if (part.MimeType == "text/html" && part.Body?.Data is not null)
            html = DecodeBase64Url(part.Body.Data);
        else if (part.MimeType == "text/plain" && part.Body?.Data is not null)
            plain = DecodeBase64Url(part.Body.Data);

        if (part.Parts is not null)
        {
            foreach (var child in part.Parts)
            {
                var (childHtml, childPlain) = ExtractBodies(child, attachments);
                html ??= childHtml;
                plain ??= childPlain;
            }
        }

        return (html, plain);
    }

    private static string DecodeBase64Url(string data)
    {
        var normalized = data.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
            normalized += new string('=', 4 - padding);

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }
}
