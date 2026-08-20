using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Authentication;

public class LoginHandler : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginHandler(
        IUserRepository userRepository,
        IDriverRepository driverRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _driverRepository = driverRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same generic failure for "no such user", "wrong password", and
        // "inactive account" — never reveal which one it was.
        if (user is null || !user.IsActive)
            return LoginOutcome.Failed();

        if (!_passwordService.Verify(user.PasswordHash, request.Password))
            return LoginOutcome.Failed();

        var driver = user.Role == Domain.Enums.UserRole.Driver
            ? await _driverRepository.GetByUserIdAsync(user.Id, cancellationToken)
            : null;

        var token = _jwtTokenService.GenerateToken(user, driver);

        var response = new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAtUtc,
            User = new AuthenticatedUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                LanguageCode = user.LanguageCode
            }
        };

        return LoginOutcome.Success(response);
    }
}
