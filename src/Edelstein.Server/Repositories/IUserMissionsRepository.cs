using Edelstein.Data.Models;

namespace Edelstein.Server.Repositories;

public interface IUserMissionsRepository
{
    Task Create(ulong xuid);
    Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid);
}
