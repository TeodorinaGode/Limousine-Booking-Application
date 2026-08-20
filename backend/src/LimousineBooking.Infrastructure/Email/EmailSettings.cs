namespace LimousineBooking.Infrastructure.Email;

/// <summary>SMTP configuration, bound from the "EmailSettings" section. Never hard-code these — production values come from environment variables/Azure configuration.</summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>When false, no external delivery is attempted — see LoggingEmailService.</summary>
    public bool Enabled { get; set; } = false;

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Limousine Service";
    public bool EnableSsl { get; set; } = true;
}
