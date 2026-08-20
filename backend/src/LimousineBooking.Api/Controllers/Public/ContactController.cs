using LimousineBooking.Application.Contact;
using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Public;

/// <summary>Anonymous public-website contact form (Prompt 17, section 19). No authentication, no account required.</summary>
[ApiController]
[Route("api/public/contact")]
[AllowAnonymous]
public class ContactController : ControllerBase
{
    private readonly IPublicContactService _publicContactService;

    public ContactController(IPublicContactService publicContactService)
    {
        _publicContactService = publicContactService;
    }

    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Submit([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _publicContactService.SubmitAsync(request, cancellationToken);
        return result.Succeeded ? Ok(new { message = "Thank you — your message has been received." }) : BadRequest(new { message = result.ErrorMessage });
    }
}
