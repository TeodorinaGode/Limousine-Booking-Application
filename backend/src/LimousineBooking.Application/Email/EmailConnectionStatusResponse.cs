namespace LimousineBooking.Application.Email;

/// <summary>
/// GET /api/admin/crm/email/connection. IsConfigured distinguishes "the server
/// has no Gmail OAuth client id/secret set at all" from "nothing has been
/// connected yet" — the frontend must show these two states differently
/// rather than a generic "disconnected".
/// </summary>
public class EmailConnectionStatusResponse
{
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public string? Provider { get; set; }
    public string? EmailAddress { get; set; }
    public string? Status { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? LastSyncError { get; set; }
}

public class AuthorizationUrlResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
}
