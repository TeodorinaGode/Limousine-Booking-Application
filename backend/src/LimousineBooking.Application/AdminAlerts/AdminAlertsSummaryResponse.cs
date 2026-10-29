namespace LimousineBooking.Application.AdminAlerts;

/// <summary>The notification bell's payload — recent activity an administrator hasn't seen yet, polled by the frontend every ~25s.</summary>
public class AdminAlertsSummaryResponse
{
    public int NewBookingsCount { get; set; }
    public IReadOnlyList<RecentBookingAlert> RecentBookings { get; set; } = Array.Empty<RecentBookingAlert>();

    public int UnreadEmailsCount { get; set; }
    public IReadOnlyList<UnreadEmailThreadAlert> RecentUnreadEmailThreads { get; set; } = Array.Empty<UnreadEmailThreadAlert>();
}

public class RecentBookingAlert
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>True if created after the admin's last visit to the bell — drives the highlight in the dropdown.</summary>
    public bool IsNew { get; set; }
}

public class UnreadEmailThreadAlert
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public DateTime LastMessageAt { get; set; }
}
