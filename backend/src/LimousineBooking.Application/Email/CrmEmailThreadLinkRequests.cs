namespace LimousineBooking.Application.Email;

public class LinkCrmEmailThreadCompanyRequest
{
    public Guid CompanyId { get; set; }
}

public class LinkCrmEmailThreadContactRequest
{
    public Guid ContactId { get; set; }
}

public class LinkCrmEmailThreadDealRequest
{
    public Guid DealId { get; set; }
}
