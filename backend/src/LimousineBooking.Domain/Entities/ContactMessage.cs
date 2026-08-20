using System.Linq;
using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A public-website contact-form submission (Prompt 17, section 19) — its own
/// small, insert-only outbox rather than being forced through the existing
/// <see cref="Notification"/> table, which requires a real <see cref="Booking"/>
/// (its <c>BookingId</c> foreign key is non-nullable — see that entity's
/// summary). A contact inquiry isn't tied to any booking, so this mirrors the
/// same transactional-outbox shape (Pending → Sent/Failed, rendered/sent only
/// by a background worker, never synchronously from the request path) without
/// borrowing or weakening Notification's booking-bound invariant.
/// </summary>
public class ContactMessage : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    /// <summary>"Phone" or "Email" — how the customer would like to be contacted back (Prompt 18, section 16). Optional; null means no preference was given.</summary>
    public string? PreferredContactMethod { get; private set; }

    /// <summary>An optional date the customer would like to be contacted by/reached (Prompt 18, section 16) — not validated against "not in the past", since this is only a soft scheduling hint for whoever follows up, not a booking commitment.</summary>
    public DateOnly? PreferredDate { get; private set; }

    public ContactMessageStatus Status { get; private set; } = ContactMessageStatus.Pending;
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private static readonly string[] AllowedContactMethods = { "Phone", "Email" };

    private ContactMessage()
    {
    }

    public ContactMessage(string name, string email, string? phone, string subject, string message, string? preferredContactMethod = null, DateOnly? preferredDate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!EmailFormat.IsValid(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));
        if (!string.IsNullOrWhiteSpace(phone) && !PhoneFormat.IsValid(phone))
            throw new ArgumentException("Phone format is invalid.", nameof(phone));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));
        if (!string.IsNullOrWhiteSpace(preferredContactMethod) && !AllowedContactMethods.Contains(preferredContactMethod, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Preferred contact method must be 'Phone' or 'Email'.", nameof(preferredContactMethod));

        Name = name;
        Email = email;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        Subject = subject;
        Message = message;
        PreferredContactMethod = string.IsNullOrWhiteSpace(preferredContactMethod) ? null : preferredContactMethod;
        PreferredDate = preferredDate;
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = ContactMessageStatus.Sent;
        SentAt = sentAtUtc;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ContactMessageStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
