using System.Text.Json;

namespace LimousineBooking.Domain.Common;

/// <summary>One recipient/sender address + optional display name — the unit stored (as a small JSON array, never a mailbox-wide blob) in the To/Cc/Bcc columns of <see cref="Entities.CrmEmailMessage"/> and <see cref="Entities.EmailSendOperation"/>.</summary>
public record EmailAddressValue(string Address, string? Name = null);

/// <summary>Serializes/deserializes the small {{Address,Name}} JSON arrays shared by every email entity that stores recipient lists.</summary>
public static class EmailAddressListSerializer
{
    public static string Serialize(IReadOnlyList<EmailAddressValue> addresses) => JsonSerializer.Serialize(addresses);

    public static IReadOnlyList<EmailAddressValue> Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? Array.Empty<EmailAddressValue>()
            : JsonSerializer.Deserialize<List<EmailAddressValue>>(json) ?? new List<EmailAddressValue>();
}
