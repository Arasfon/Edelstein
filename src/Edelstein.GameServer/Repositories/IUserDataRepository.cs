using Edelstein.Data.Models;

namespace Edelstein.GameServer.Repositories;

public interface IUserDataRepository
{
    public Task<UserData?> GetByXuid(ulong xuid);
    public Task UpdateTutorialStep(ulong xuid, uint step);
}
