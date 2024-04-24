using Edelstein.Data.Models.Components;
using Edelstein.Server.Models;

namespace Edelstein.Server.Services;

public interface ILotteryService
{
    public Task<List<Lottery>> GetAndRefreshUserLotteriesData(ulong xuid);
    public Task<Lottery> GetTutorialLotteryByMasterCharacterId(uint masterCharacterId);
    public Task<LotteryDrawResult> Draw(ulong xuid, Lottery lottery);
    public Task<bool> IsTutorial(Lottery lottery);
}
