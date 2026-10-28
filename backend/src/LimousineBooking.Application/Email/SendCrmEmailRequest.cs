using LimousineBooking.Domain.Common;

namespace LimousineBooking.Application.Email;

/// <summary>
/// One compose-and-send request. ClientOperationId comes from the route (see
/// CrmEmailSendController) rather than this body — the frontend must keep
/// reusing the same id across any retry of the same attempt so the backend
/// can recognize a replay instead of sending twice.
/// </summary>
public class SendCrmEmailRequest
{
    /// <summary>When replying within an existing synced conversation — keeps the reply in the same Gmail thread and inherits its Company/Contact/Deal associations as defaults.</summary>
    public Guid? ThreadId { get; set; }

    public List<EmailAddressValue> ToAddresses { get; set; } = new();
    public List<EmailAddressValue>? CcAddresses { get; set; }
    public List<EmailAddressValue>? BccAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
}
