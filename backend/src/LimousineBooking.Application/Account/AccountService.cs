using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Account;

/// <inheritdoc cref="IAccountService" />
public class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;

    public AccountService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AccountPreferencesResponse?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : new AccountPreferencesResponse { LanguageCode = user.LanguageCode };
    }

    public async Task<AccountPreferencesResponse?> UpdatePreferencesAsync(Guid userId, UpdateAccountPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        user.SetLanguage(request.LanguageCode);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AccountPreferencesResponse { LanguageCode = user.LanguageCode };
    }
}
