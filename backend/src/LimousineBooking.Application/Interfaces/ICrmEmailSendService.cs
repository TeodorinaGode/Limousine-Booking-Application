using LimousineBooking.Application.Email;

namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Composes and sends a real email through the connected provider. Keyed by
/// a frontend-generated clientOperationId so the same compose-send attempt
/// can be safely retried (including across a page reload) without ever
/// double-sending — see EmailSendOperation.
/// </summary>
public interface ICrmEmailSendService
{
    Task<CrmEmailSendResult> SendAsync(Guid clientOperationId, SendCrmEmailRequest request, Guid userId, CancellationToken cancellationToken = default);
}
