using Edelstein.Data.Models;

namespace Edelstein.GameServer.Repositories;

public interface IUserMissionsRepository
{
    Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid);
}
