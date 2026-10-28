using System.Text;
using LimousineBooking.Application.Email;
using LimousineBooking.Domain.Common;

namespace LimousineBooking.Infrastructure.Email;

/// <summary>
/// Builds the raw RFC 2822 MIME message Gmail's messages.send expects as a
/// base64url string. No attachments → a single text/html part. With
/// attachments → multipart/mixed wrapping the html part plus one base64 part
/// per attachment. The embedded Message-ID is what later lets an ambiguous
/// send outcome be reconciled via FindMessageByMessageIdHeaderAsync instead of
/// blindly resending.
/// </summary>
internal static class GmailMimeMessageBuilder
{
    public static string BuildRawBase64Url(OutgoingEmailMessage message)
    {
        var boundary = $"boundary_{Guid.NewGuid():N}";
        var builder = new StringBuilder();

        builder.Append("MIME-Version: 1.0\r\n");
        builder.Append($"Message-ID: {message.MessageIdHeader}\r\n");
        builder.Append($"From: {message.FromAddress}\r\n");
        builder.Append($"To: {FormatAddressList(message.ToAddresses)}\r\n");
        if (message.CcAddresses is { Count: > 0 })
            builder.Append($"Cc: {FormatAddressList(message.CcAddresses)}\r\n");
        if (message.BccAddresses is { Count: > 0 })
            builder.Append($"Bcc: {FormatAddressList(message.BccAddresses)}\r\n");
        builder.Append($"Subject: {EncodeHeaderValue(message.Subject)}\r\n");

        var hasAttachments = message.Attachments is { Count: > 0 };

        if (hasAttachments)
        {
            builder.Append($"Content-Type: multipart/mixed; boundary=\"{boundary}\"\r\n\r\n");
            builder.Append($"--{boundary}\r\n");
            builder.Append("Content-Type: text/html; charset=\"UTF-8\"\r\n");
            builder.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            AppendBase64Body(builder, message.BodyHtml);

            foreach (var attachment in message.Attachments!)
            {
                builder.Append($"\r\n--{boundary}\r\n");
                builder.Append($"Content-Type: {attachment.ContentType}; name=\"{attachment.FileName}\"\r\n");
                builder.Append("Content-Transfer-Encoding: base64\r\n");
                builder.Append($"Content-Disposition: attachment; filename=\"{attachment.FileName}\"\r\n\r\n");
                builder.Append(Convert.ToBase64String(attachment.Content, Base64FormattingOptions.InsertLineBreaks));
                builder.Append("\r\n");
            }

            builder.Append($"--{boundary}--");
        }
        else
        {
            builder.Append("Content-Type: text/html; charset=\"UTF-8\"\r\n");
            builder.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            AppendBase64Body(builder, message.BodyHtml);
        }

        var rawBytes = Encoding.UTF8.GetBytes(builder.ToString());
        return Convert.ToBase64String(rawBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static void AppendBase64Body(StringBuilder builder, string html) =>
        builder.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(html ?? string.Empty), Base64FormattingOptions.InsertLineBreaks));

    private static string FormatAddressList(IReadOnlyList<EmailAddressValue> addresses) =>
        string.Join(", ", addresses.Select(a => string.IsNullOrWhiteSpace(a.Name) ? a.Address : $"\"{a.Name}\" <{a.Address}>"));

    /// <summary>RFC 2047-encodes the subject so non-ASCII characters survive — a plain UTF-8 Subject header is not valid RFC 2822.</summary>
    private static string EncodeHeaderValue(string value) =>
        value.Any(c => c > 127) ? $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=" : value;
}
