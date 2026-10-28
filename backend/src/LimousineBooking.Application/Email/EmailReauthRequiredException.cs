namespace LimousineBooking.Application.Email;

/// <summary>Thrown when a provider rejects a refresh attempt because the refresh token itself was revoked/expired — the connection must be marked ReauthRequired, not just logged as a transient failure.</summary>
public class EmailReauthRequiredException : Exception
{
    public EmailReauthRequiredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public EmailReauthRequiredException(string message) : base(message)
    {
    }
}
