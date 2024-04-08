using Edelstein.Data.Models;

namespace Edelstein.GameServer.Repositories;

public interface IUserHomeRepository
{
    public Task<UserHomeDocument?> GetByXuid(ulong xuid);
    public Task InitializePresets(ulong xuid, uint masterCardId);
}
