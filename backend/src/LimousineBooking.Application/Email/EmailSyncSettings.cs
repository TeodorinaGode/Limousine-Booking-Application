namespace LimousineBooking.Application.Email;

/// <summary>Bound from the "EmailSyncSettings" section — mirrors NotificationSettings' shape for the background poller.</summary>
public class EmailSyncSettings
{
    public const string SectionName = "EmailSyncSettings";

    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>A checkpoint claimed (IsRunning=true) longer than this is assumed crashed and reclaimed — same pattern as NotificationSettings.StaleProcessingMinutes.</summary>
    public int StaleRunningMinutes { get; set; } = 10;
}
