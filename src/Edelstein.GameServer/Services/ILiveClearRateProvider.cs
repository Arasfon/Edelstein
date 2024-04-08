using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Services;

public interface ILiveClearRateProvider
{
    public Task<List<AllUserClearRate>> GetAll();
}
