using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Crm;

public class AdministratorDirectoryService : IAdministratorDirectoryService
{
    private readonly IUserRepository _userRepository;

    public AdministratorDirectoryService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<AdministratorSummaryResponse>> GetAdministratorsAsync(CancellationToken cancellationToken = default)
    {
        var administrators = await _userRepository.GetAdministratorsAsync(cancellationToken);

        return administrators
            .Select(u => new AdministratorSummaryResponse { Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, Email = u.Email })
            .ToList();
    }
}
