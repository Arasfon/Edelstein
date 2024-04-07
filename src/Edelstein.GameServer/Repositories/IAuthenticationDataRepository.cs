using Edelstein.Data.Models;

namespace Edelstein.GameServer.Repositories;

public interface IAuthenticationDataRepository
{
    public Task<AuthenticationData?> GetByXuid(ulong xuid);
}
