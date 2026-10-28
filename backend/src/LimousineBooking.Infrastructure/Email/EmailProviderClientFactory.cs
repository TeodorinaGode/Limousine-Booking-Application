using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Infrastructure.Email;

public class EmailProviderClientFactory : IEmailProviderClientFactory
{
    private readonly GmailProviderClient _gmailClient;

    public EmailProviderClientFactory(GmailProviderClient gmailClient)
    {
        _gmailClient = gmailClient;
    }

    public IEmailProviderClient GetClient(EmailProvider provider) => provider switch
    {
        EmailProvider.Gmail => _gmailClient,
        EmailProvider.MicrosoftGraph => throw new NotSupportedException("Microsoft Graph is reserved for Stage 2B and is not implemented yet."),
        _ => throw new NotSupportedException($"Unknown email provider '{provider}'.")
    };
}
