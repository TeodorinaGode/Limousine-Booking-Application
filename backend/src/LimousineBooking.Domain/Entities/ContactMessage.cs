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

    public ContactMessageStatus Status { get; private set; } = ContactMessageStatus.Pending;
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ContactMessage()
    {
    }

    public ContactMessage(string name, string email, string? phone, string subject, string message)
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

        Name = name;
        Email = email;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        Subject = subject;
        Message = message;
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
