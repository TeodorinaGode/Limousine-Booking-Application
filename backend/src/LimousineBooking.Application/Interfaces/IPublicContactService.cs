using LimousineBooking.Application.Contact;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Handles anonymous public-website contact-form submissions (Prompt 17, section 19).</summary>
public interface IPublicContactService
{
    Task<ContactOperationResult> SubmitAsync(ContactRequest request, CancellationToken cancellationToken = default);
}
