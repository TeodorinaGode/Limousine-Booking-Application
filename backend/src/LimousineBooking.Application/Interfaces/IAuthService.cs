using LimousineBooking.Application.Authentication;

namespace LimousineBooking.Application.Interfaces;

public interface IAuthService
{
    Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
