using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Lottery;

public record DrawLotteryResponseData(
    IEnumerable<LotteryItem> LotteryItemList,
    UpdatedValueList UpdatedValueList,
    List<Gift> GiftList,
    List<uint> ClearMissionIds,
    List<LotteryDrawCount> DrawCountList
);
