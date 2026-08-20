namespace LimousineBooking.Application.Authentication;

public class AuthenticatedUserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>Null if the user has never saved a preference — the frontend then falls back to the browser's language.</summary>
    public string? LanguageCode { get; set; }
}
