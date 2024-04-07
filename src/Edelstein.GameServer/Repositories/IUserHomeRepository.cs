using Edelstein.Data.Models;

namespace Edelstein.GameServer.Repositories;

public interface IUserHomeRepository
{
    public Task<UserHomeDocument?> GetByXuid(ulong xuid);
}
