using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Lottery;

public record DrawLotteryResponseData(
    List<LotteryItem> LotteryItemList,
    UpdatedValueList UpdatedValueList,
    List<Gift> GiftList,
    List<uint> ClearMissionIds,
    List<LotteryDrawCount> DrawCountList
);
