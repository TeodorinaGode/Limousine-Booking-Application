namespace LimousineBooking.Application.Notifications;

/// <summary>Business-level notification configuration, bound from the "NotificationSettings" section.</summary>
public class NotificationSettings
{
    public const string SectionName = "NotificationSettings";

    /// <summary>Where admin-facing operational notifications (e.g. "requires manual assignment") are sent.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    public int MaxRetries { get; set; } = 5;

    /// <summary>Backoff delay (minutes) indexed by retry count — index 0 is the delay before the 1st retry, etc.</summary>
    public int[] RetryBackoffMinutes { get; set; } = { 1, 5, 15, 30, 60 };

    /// <summary>How often the background worker polls for due messages.</summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>A Processing message older than this is assumed crashed and re-claimed.</summary>
    public int StaleProcessingMinutes { get; set; } = 5;
}
