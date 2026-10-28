namespace LimousineBooking.Domain.Enums;

/// <summary>Gmail is implemented in Stage 2A. MicrosoftGraph is reserved so Stage 2B can add it behind the same IEmailProviderClient interface.</summary>
public enum EmailProvider
{
    Gmail,
    MicrosoftGraph
}
