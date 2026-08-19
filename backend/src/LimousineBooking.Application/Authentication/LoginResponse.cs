namespace LimousineBooking.Application.Authentication;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthenticatedUserResponse User { get; set; } = new();
}
