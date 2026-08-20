using LimousineBooking.Application.Interfaces;
using DomainContactMessage = LimousineBooking.Domain.Entities.ContactMessage;

namespace LimousineBooking.Application.Contact;

/// <inheritdoc cref="IPublicContactService" />
public class PublicContactService : IPublicContactService
{
    private const int MaxNameLength = 100;
    private const int MaxSubjectLength = 200;
    private const int MinMessageLength = 10;
    private const int MaxMessageLength = 2000;

    private readonly IContactMessageRepository _contactMessageRepository;

    public PublicContactService(IContactMessageRepository contactMessageRepository)
    {
        _contactMessageRepository = contactMessageRepository;
    }

    public async Task<ContactOperationResult> SubmitAsync(ContactRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var subject = request.Subject?.Trim() ?? string.Empty;
        var message = request.Message?.Trim() ?? string.Empty;

        var validationError = Validate(name, subject, message);
        if (validationError is not null)
            return ContactOperationResult.Failure(validationError);

        DomainContactMessage contactMessage;
        try
        {
            contactMessage = new DomainContactMessage(name, email, phone, subject, message);
        }
        catch (ArgumentException ex)
        {
            return ContactOperationResult.Failure(ex.Message);
        }

        await _contactMessageRepository.AddAsync(contactMessage, cancellationToken);
        await _contactMessageRepository.SaveChangesAsync(cancellationToken);

        return ContactOperationResult.Success();
    }

    /// <summary>
    /// Length limits and a simple "reject angle brackets" guard (section 18's
    /// "do not allow arbitrary HTML") — beyond that, <see cref="DomainContactMessage"/>'s
    /// constructor already enforces required fields + email/phone format, so this
    /// only checks what the domain entity doesn't (max lengths, the HTML guard).
    /// </summary>
    private static string? Validate(string name, string subject, string message)
    {
        if (name.Length > MaxNameLength)
            return $"Name must not exceed {MaxNameLength} characters.";
        if (subject.Length > MaxSubjectLength)
            return $"Subject must not exceed {MaxSubjectLength} characters.";
        if (message.Length < MinMessageLength)
            return $"Message must be at least {MinMessageLength} characters.";
        if (message.Length > MaxMessageLength)
            return $"Message must not exceed {MaxMessageLength} characters.";
        if (ContainsHtml(name) || ContainsHtml(subject) || ContainsHtml(message))
            return "HTML markup is not allowed.";

        return null;
    }

    private static bool ContainsHtml(string value) => value.Contains('<') || value.Contains('>');
}
