using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Services;

public interface ILotteryService
{
    public Task<List<Lottery>> GetAndRefreshUserLotteriesData(ulong xuid);
    public Task<Lottery> GetTutorialLotteryByMasterCharacterId(uint masterCharacterId);
    public Task<LotteryDrawResult> Draw(ulong xuid, Lottery lottery);
    public Task<bool> IsTutorial(Lottery lottery);
}
