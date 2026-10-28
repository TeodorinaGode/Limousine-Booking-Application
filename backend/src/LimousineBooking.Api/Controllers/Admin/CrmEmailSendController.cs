using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Admin;

/// <summary>
/// Real email sending. One send-operation resource per clientOperationId —
/// PUT is idempotent by design: the frontend keeps reusing the same id
/// across any retry of the same compose attempt, and the backend recognizes
/// a replay instead of sending twice. See CrmEmailSendService.
/// </summary>
[ApiController]
[Route("api/admin/crm/email/send-operations")]
[Authorize(Roles = "Administrator")]
public class CrmEmailSendController : ControllerBase
{
    private readonly ICrmEmailSendService _sendService;
    private readonly ICurrentUserService _currentUserService;

    public CrmEmailSendController(ICrmEmailSendService sendService, ICurrentUserService currentUserService)
    {
        _sendService = sendService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{clientOperationId:guid}")]
    public async Task<ActionResult<CrmEmailSendResultResponse>> Send(Guid clientOperationId, [FromBody] SendCrmEmailRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized(new { message = "Unable to resolve the current user." });

        var result = await _sendService.SendAsync(clientOperationId, request, userId, cancellationToken);

        var response = new CrmEmailSendResultResponse { Status = result.Status.ToString(), MessageId = result.MessageId };

        if (result.Succeeded)
            return Ok(response);

        // Submitting means the provider call itself was ambiguous (timeout/5xx) —
        // 202 says "still processing, retry the same clientOperationId to check," which
        // Failed (a clean provider rejection) is not: that's a definite, safe-to-edit-and-retry failure.
        if (result.Status == Domain.Enums.EmailSendStatus.Submitting)
            return StatusCode(202, response);

        return result.Error switch
        {
            CrmEmailSendError.NotConnected => StatusCode(503, new { message = result.ErrorMessage }),
            CrmEmailSendError.Validation => BadRequest(new { message = result.ErrorMessage }),
            _ => StatusCode(502, new { message = result.ErrorMessage })
        };
    }
}

public class CrmEmailSendResultResponse
{
    public string Status { get; set; } = string.Empty;
    public Guid? MessageId { get; set; }
}
