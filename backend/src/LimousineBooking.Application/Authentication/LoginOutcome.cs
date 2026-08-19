namespace LimousineBooking.Application.Authentication;

/// <summary>
/// Result of a login attempt. Deliberately does not distinguish *why* a login
/// failed (unknown email, wrong password, inactive account) so callers cannot
/// leak that information to the client and enable user enumeration.
/// </summary>
public class LoginOutcome
{
    public bool Succeeded { get; }
    public LoginResponse? Response { get; }

    private LoginOutcome(bool succeeded, LoginResponse? response)
    {
        Succeeded = succeeded;
        Response = response;
    }

    public static LoginOutcome Success(LoginResponse response) => new(true, response);

    public static LoginOutcome Failed() => new(false, null);
}
