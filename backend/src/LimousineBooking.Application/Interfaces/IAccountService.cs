using LimousineBooking.Application.Account;

namespace LimousineBooking.Application.Interfaces;

/// <summary>Self-service preferences for the currently authenticated user (Administrator or Driver) — see Prompt 16 section 21.</summary>
public interface IAccountService
{
    Task<AccountPreferencesResponse?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AccountPreferencesResponse?> UpdatePreferencesAsync(Guid userId, UpdateAccountPreferencesRequest request, CancellationToken cancellationToken = default);
}
