using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using OneOf;

namespace Edelstein.GameServer.Services;

public interface ILotteryService
{
    public Task<Lottery> GetTutorialLotteryByMasterCharacterId(uint masterCharacterId);
    public Task<OneOf<LotteryDrawResult, TutorialLotteryDrawResult>> Draw(Lottery lottery);
}
