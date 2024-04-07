using Edelstein.Data.Models;

namespace Edelstein.GameServer.Services;

public interface IUserService
{
    public Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid);
    public Task<UserData?> GetUserDataByXuid(ulong xuid);

    public Task ProgressTutorial(ulong userXuid, uint step);
}
