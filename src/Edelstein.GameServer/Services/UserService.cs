using Edelstein.Data.Models;
using Edelstein.GameServer.Repositories;

namespace Edelstein.GameServer.Services;

public class UserService : IUserService
{
    private readonly IAuthenticationDataRepository _authenticationDataRepository;
    private readonly IUserDataRepository _userDataRepository;

    public UserService(IAuthenticationDataRepository authenticationDataRepository, IUserDataRepository userDataRepository)
    {
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
    }

    public async Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid) =>
        await _authenticationDataRepository.GetByXuid(xuid);

    public async Task<UserData?> GetUserDataByXuid(ulong xuid) =>
        await _userDataRepository.GetByXuid(xuid);

    public async Task ProgressTutorial(ulong userXuid, uint step) =>
        await _userDataRepository.UpdateTutorialStep(userXuid, step);
}
