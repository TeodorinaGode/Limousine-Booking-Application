namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Signs/validates the OAuth "state" parameter — the standard CSRF defense
/// that proves an OAuth callback actually originated from an authorization
/// URL this server built, not a forged request pointed at the callback.
/// Embeds the initiating admin's user id, since the callback itself is a
/// plain browser redirect from the provider with no bearer token — the
/// signed state is the only way to know who started the connect flow.
/// </summary>
public interface IOAuthStateService
{
    string CreateState(Guid userId);

    bool TryValidateState(string state, out Guid userId);
}
