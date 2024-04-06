using Edelstein.Data.Models;

namespace Edelstein.PaymentServer.Repositories;

public interface IUserDataRepository
{
    public Task<UserData> CreateTutorialUserData(ulong xuid);
}
